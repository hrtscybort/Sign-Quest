using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    public class PlayerTurn : State
{
    private string currentActionWord;
    public string CurrentActionWord => currentActionWord;
    private bool waitingForSign = false;
    public bool WaitingForSign => waitingForSign;

    public PlayerTurn(BattleSystem battleSystem) : base(battleSystem)
    {
    }

    public override IEnumerator Start()
    {
        Debug.Log("Choose an action.");
        
        if (BattleSystem.Player.CurrentMana < BattleSystem.Player.MaxMana)
        {
            BattleSystem.Interface.DisableSpecialButton();
        }
        else
        {
            BattleSystem.Interface.EnableSpecialButton();
        }

        waitingForSign = false;
        yield break;
    }

    public IEnumerator PerformSignedAction(string actionType)
    {
        BattleSystem.signUI.Show(BattleSystem.GetPseudoRandomWord());
        waitingForSign = true;

        while (!BattleSystem.signUI.IsDone)
        {
            yield return null;
        }

        float accuracy = BattleSystem.signUI.Score;
        BattleSystem.signUI.Hide();
        waitingForSign = false;

        switch (actionType)
        {
            case "attack":
                yield return BattleSystem.StartCoroutine(SignedAttack(accuracy));
                break;
            case "heal":
                yield return BattleSystem.StartCoroutine(SignedHeal(accuracy));
                break;
            case "defend":
                yield return BattleSystem.StartCoroutine(SignedDefend(accuracy));
                break;
        }
    }

    private IEnumerator SignedAttack(float accuracy)
    {
        int damage = Mathf.RoundToInt(BattleSystem.Player.Attack * accuracy);
        var isDead = BattleSystem.Enemy.Damage(damage);

        BattleSystem.Player.Charge(BattleSystem.Player.Charging);

        if (accuracy == 1f) {
            BattleSystem.Manager.RegisterPerfectSign(true);
        } else {
            BattleSystem.Manager.RegisterPerfectSign(false);
        }
       
        ShowFeedback(accuracy);

        yield return new WaitForSeconds(1f);

        if (isDead)
        {
            BattleSystem.SetState(new Won(BattleSystem));
        }
        else
        {
            BattleSystem.SetState(new EnemyTurn(BattleSystem));
        }
    }

    private IEnumerator SignedHeal(float accuracy)
    {
        int healAmount = Mathf.RoundToInt(BattleSystem.Player.Healing * accuracy);
        BattleSystem.Player.Heal(healAmount);

        if (accuracy == 1f) {
            BattleSystem.Manager.RegisterPerfectSign(true);
        } else {
            BattleSystem.Manager.RegisterPerfectSign(false);
        }

        ShowFeedback(accuracy);

        yield return new WaitForSeconds(1f);
        BattleSystem.SetState(new EnemyTurn(BattleSystem));
    }

    private IEnumerator SignedDefend(float accuracy)
    {
        int defenseBoost = Mathf.RoundToInt(BattleSystem.Player.Defending * accuracy);
        BattleSystem.Player.Defend(defenseBoost);

        if (accuracy == 1f) {
            BattleSystem.Manager.RegisterPerfectSign(true);
        } else {
            BattleSystem.Manager.RegisterPerfectSign(false);
        }

        ShowFeedback(accuracy);

        yield return new WaitForSeconds(1f);
        BattleSystem.SetState(new EnemyTurn(BattleSystem));
    }

    public override IEnumerator Special()
    {
        if (BattleSystem.Player.CurrentMana < BattleSystem.Player.MaxMana)
        {
            Debug.Log("Need more mana!");
            yield break;
        }

        int specialDamage = BattleSystem.Player.Special;
        var isDead = BattleSystem.Enemy.Damage(specialDamage);
        BattleSystem.Player.Discharge();

        yield return new WaitForSeconds(1f);

        if (isDead)
        {
            BattleSystem.SetState(new Won(BattleSystem));
        }
        else
        {
            BattleSystem.SetState(new EnemyTurn(BattleSystem));
        }
    }

    // Modify the existing action methods to use signing
    public override IEnumerator Attack()
    {
        return PerformSignedAction("attack");
    }

    public override IEnumerator Heal()
    {
        return PerformSignedAction("heal");
    }

    public override IEnumerator Defend()
    {
        return PerformSignedAction("defend");
    }

    private void ShowFeedback(float accuracy)
    {
        string message;
        Color color;

        if (BattleSystem.Data.CorrectHandshape && BattleSystem.Data.CorrectLocation)
        {
            message = "Perfect!";
            color = Color.green;
        }
        else if (!BattleSystem.Data.CorrectHandshape && BattleSystem.Data.CorrectLocation)
        {
            message = "Wrong \n Handshape!";
            color = new Color(1f, 0.85f, 0f); // yellow-ish
        }
        else if (!BattleSystem.Data.CorrectLocation && BattleSystem.Data.CorrectHandshape)
        {
            message = "Wrong \n Location!";
            color = new Color(1f, 0.85f, 0f); // yellow-ish
        }
        else
        {
            message = "Wrong!";
            color = Color.red;
        }

        var feedbackGO = GameObject.Instantiate(
            BattleSystem.feedbackMessagePrefab,
            BattleSystem.playerHeadTransform.position,
            Quaternion.identity
        );
        feedbackGO.transform.SetParent(BattleSystem.playerHeadTransform, true);
        feedbackGO.GetComponent<FeedbackTextUI>().Show(message, color);
    }
}
}