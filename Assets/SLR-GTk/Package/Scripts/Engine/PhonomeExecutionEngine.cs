using Common;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using TreeEditor;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Serialization;
using RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode;

namespace Engine {
    public class PhonemeExecutionEngine : MonoBehaviour
    {
        [SerializeField] private Preview.UnityMpHandPreviewPainter screen;
        [SerializeField] private Camera.StreamCamera inputCamera;

        public SLRPhonemeModel recognizer;
        public Buffer<HandLandmarkerResult> buffer;
        public MediapipeHandModelManager posePredictor;

        [FormerlySerializedAs("_modelFile")] [SerializeField] private TextAsset modelFile;

        //[FormerlySerializedAs("_metadataFile")] [SerializeField] private TextAsset metadataFile;

        [FormerlySerializedAs("_indexToCategory")] [SerializeField] private TextAsset idx_to_category_file;
        [FormerlySerializedAs("_indexToHandshape")] [SerializeField] private TextAsset idx_to_handshape_file;
        [FormerlySerializedAs("_indexToMinorLocatiion")] [SerializeField] private TextAsset idx_to_minor_loc_file;

        [FormerlySerializedAs("_categoryToHandshape")] [SerializeField] private TextAsset category_to_handshape_file;
        [FormerlySerializedAs("_categoryToMinorLocation")] [SerializeField] private TextAsset category_to_minor_loc_file;

        [FormerlySerializedAs("_mediapipeGraph")] [SerializeField] private TextAsset mediapipeGraph;
        [FormerlySerializedAs("_isInterpolating")] [SerializeField] private bool isInterpolating;

        private float lastTime = -1;

        private Dictionary<int, string> idx_to_category;
        private Dictionary<int, string> idx_to_handshape;
        private Dictionary<int, string> idx_to_minor_loc;

        private Dictionary<string, string> category_to_handshape;

        private Dictionary<string, string> category_to_minor_location;
        
        private static class Config
        {
            public static readonly int NumInputFrames = 80;
            public static readonly int NumInputPoints = 21;
        }

        void Start()
        {
            buffer = new Buffer<HandLandmarkerResult>();

            /*
            string[] mapping = mappingFile.text.Split("\n");

            for (int i = 0; i < mapping.Length; i++)
            {
                mapping[i] = mapping[i].Trim().ToLower();
            }
            */

            //this.metadata = JsonConvert.DeserializeObject<Metadata>(metadataFile.text);

            // Index -> Category conversion tables
            this.idx_to_category = JsonConvert.DeserializeObject<Dictionary<int, string>>(idx_to_category_file.text);
            this.idx_to_handshape = JsonConvert.DeserializeObject<Dictionary<int, string>>(idx_to_handshape_file.text);
            this.idx_to_minor_loc = JsonConvert.DeserializeObject<Dictionary<int, string>>(idx_to_minor_loc_file.text);

            this.category_to_handshape = JsonConvert.DeserializeObject<Dictionary<string, string>>(category_to_handshape_file.text);
            this.category_to_minor_location = JsonConvert.DeserializeObject<Dictionary<string, string>>(category_to_minor_loc_file.text);


            //recognizer = new SLRTfLitePhonemeModel<string>(modelFile, new List<string>(mapping));

            recognizer = new SLRPhonemeModel(modelFile, idx_to_category, idx_to_handshape, idx_to_minor_loc);

            //Creates the mediapipe recognizer object
            posePredictor = new MediapipeHandModelManager(mediapipeGraph.bytes, RunningMode.LIVE_STREAM);

            posePredictor.AddCallback("buffer", result =>
            {                        
                if (result.Result.handLandmarks == null || result.Result.handLandmarks.Count <= 0 ||
                    result.Result.handLandmarks[0].landmarks.Count <= 0) { }
                else {
                    buffer.AddElement(result.Result);
                }
                if (screen) screen.UpdateLandmarks(result.Result);
                if (result.Image != null)
                    if (screen) screen.UpdateImage(result.Image);
                    else
                        Debug.Log("Got null screen");
            });

            buffer.AddCallback("trigger", bufferedResults =>
            {
                List<float> inputArray = new List<float>();

                if (bufferedResults.Count <= 0) return;
                
                
                if (bufferedResults.Count >= 80)
                {
                    //Debug.Log("Overfill");
                    var lastFrames = bufferedResults.GetRange(bufferedResults.Count - Config.NumInputFrames, Config.NumInputFrames);
                    //Debug.Log("Buffer Gets " + lastFrames.Count);
                    foreach (var landmark in lastFrames)
                    {
                        for (int j = 0; j < Config.NumInputPoints; j++)
                        {
                            inputArray.Add(1 - landmark.handLandmarks[0].landmarks[j].x);
                            inputArray.Add(1 - landmark.handLandmarks[0].landmarks[j].y);
                        }
                    }
                }

                List<float> interpolatedInput = TemporalLinearInterpolateFrames(inputArray);
                int expectedInputSize = Config.NumInputFrames * Config.NumInputPoints * 2;
                if (interpolatedInput.Count != expectedInputSize)
                {
                    Debug.LogError($"Expected {expectedInputSize} entries for input array but got {interpolatedInput.Count} entries.");
                    return;
                }

                //Debug.Log("Input array got " + inputArray.Count);

                if (inputArray.Count > 0)
                {
                    recognizer.RunModel(inputArray.ToArray());
                }
                buffer.Clear();
            });

            recognizer.AddCallback("default", (translation) => {
               // Debug.Log(translation);
            });

            if (inputCamera) inputCamera.AddCallback("default", image => {
                    posePredictor.Single(image, (int)(Time.realtimeSinceStartup * 1000));
            });
        
            if (screen) screen.Show();

            buffer.trigger = new NoTrigger<HandLandmarkerResult>();
            Poll();
        }

        public void Poll() {
            inputCamera.Poll();
        }

        public void Pause() {
            inputCamera.Pause();
            buffer.TriggerCallbacks();
        }

        public void Toggle() {
            if (screen.Visible) screen.Hide();
            else screen.Show();
        }
        static List<string> MakeLowercase(List<string> words)
        {
            for (int i = 0; i < words.Count; i++)
            {
                words[i] = words[i].ToLower();
            }
            return words;
        }

        // Implementated with reference to PyTorch's implementation of torch.nn.functional.interpolate function.
        private List<float> TemporalLinearInterpolateFrames(List<float> inputArray)
        {
            int frameSize = Config.NumInputPoints * 2;
            if (inputArray.Count % frameSize != 0)
            {
                Debug.Log($"Invalid input size for interpolation. Array must contain data in sizes of {frameSize}.");
                return inputArray;
            }

            List<float> linearlyInterpolatedLandmarks = new List<float>();

            int inputFrames = inputArray.Count / frameSize;
            float interpolationSpacing = ((float)inputFrames) / Config.NumInputFrames;

            for (int frame = 0; frame < Config.NumInputFrames; frame++)
            {
                // Given current frame, referenceFrame calculates which frame to interpolate off of
                float referenceFrame = (frame + 0.5f) * interpolationSpacing - 0.5f;
                if (referenceFrame < 0) { referenceFrame = 0; }
                if (referenceFrame > inputFrames - 1) { referenceFrame = inputFrames - 1; }

                int curFrameIndex = Mathf.FloorToInt(referenceFrame);
                int nextFrameIndex = (curFrameIndex < inputFrames - 1) ? curFrameIndex + 1 : curFrameIndex ;
                
                int curFrameLandmarkRange = curFrameIndex * frameSize;
                int nextFrameLandmarkRange = nextFrameIndex * frameSize;

                float t = referenceFrame - curFrameIndex;

                for (int landmark = 0; landmark < frameSize; landmark += 2)
                {
                    float xInitial = inputArray[curFrameLandmarkRange + landmark];
                    float yInitial = inputArray[curFrameLandmarkRange + landmark + 1];

                    float xFinal = inputArray[nextFrameLandmarkRange + landmark];
                    float yFinal = inputArray[nextFrameLandmarkRange + landmark + 1];

                    float xLerp = (1 - t) * xInitial + t * xFinal;
                    float yLerp = (1 - t) * yInitial + t * yFinal;

                    linearlyInterpolatedLandmarks.Add(xLerp);
                    linearlyInterpolatedLandmarks.Add(yLerp);
                }

                Debug.Log($"Linearly interpolated frame: {frame + 1} / {Config.NumInputFrames}");
            }

            return linearlyInterpolatedLandmarks;
        }
    }
}
