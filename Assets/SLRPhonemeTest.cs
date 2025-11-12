using UnityEngine;
using Engine; 
using System.Collections.Generic;
using Common;
using System;
using Model;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Assets.Scripts.Combat;
using Newtonsoft.Json;

public class SLRPhonemeTest : MonoBehaviour
{
    public PhonemeExecutionEngine engine;
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private ExpectedSignsDatabase signsDatabase;
    [SerializeField] private SignPromptUI signUI;
    [SerializeField] private GameObject signUIGO;
    private bool init;

    private int frame = 0;
    private List<string> levelSigns = new List<string>();

    void Update()
    {
        if (signUIGO.activeSelf) {
            if(!init) {
                // where initialization goes
                Debug.Log("Added Callback");

                string[] currentVocab = battleSystem.GetVocabForCurrentWave();
                levelSigns.Clear();
                levelSigns.AddRange(currentVocab);
                Debug.Log("Level Signs: " + string.Join(", ", levelSigns));

                engine.recognizer.outputFilters.Clear();
                engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
                engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
                Debug.Log(battleSystem.signUI.WordText.text);
                engine.recognizer.AddCallback("print", pred => {
                    Debug.Log("Got Pred: " + pred);
                    if (pred.gloss != "None") {
                        OnSignDetected(pred);
                    }
                });

                init = true;
            }
        }

        /*
        if (frame == 100) {
             frame = 0;
             //Debug.Log("Adding Callback!");
             engine.buffer.TriggerCallbacks();
             engine.recognizer.AddCallback("print", pred => Debug.Log("Got Pred: " + pred));
        }
        else frame++;
        */
        //Debug.Log(frame);
        engine.buffer.trigger = new CapacityFullTrigger<HandLandmarkerResult>(80);
        /*
        if (battleSystem.CurrentState is PlayerTurn playerTurn && playerTurn.WaitingForSign) {
            engine.buffer.TriggerCallbacks();
        }
        */
        /*
        if (Input.GetKeyDown(KeyCode.Space)) {
            OnButtonPressed();
        }
        */
    }

    void OnButtonPressed()
    {
        engine.buffer.TriggerCallbacks();
    }

    private void OnSignDetected(PhonemePrediction result)
    {
        // if (battleSystem.CurrState is PlayerTurn playerTurn && playerTurn.WaitingForSign)
        // {
            bool isCorrectWord = result.gloss == battleSystem.signUI.WordText.text;
            bool correctHandshape = signsDatabase.CorrectHandshape; 
            bool correctLocation = signsDatabase.CorrectLocation;
            
            Debug.Log($"Processing sign: {result.gloss}");
            
            if (isCorrectWord)
            {
                int correctFields = signsDatabase.CountCorrectFields(result);
                float score = correctFields / 2f;

                if (signUI.tutorialPressed) {
                    battleSystem.signUI.Finish(0.1f);
                }
                else {
                    battleSystem.signUI.Finish(score);
                }
            }
            else
            {
                battleSystem.signUI.Finish(0.1f);
            }
        }
    // }
}
