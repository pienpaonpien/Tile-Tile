using System.Collections;
using UnityEngine;

public class StoryInputGate : MonoBehaviour
{
    private Player[] players;
    private bool[] playerWasEnabled;
    private Hand[] hands;
    private bool[] handWasEnabled;

    public void DisableGameplayInput()
    {
        players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        playerWasEnabled = new bool[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            if (player == null)
            {
                continue;
            }

            playerWasEnabled[i] = player.enabled;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            player.enabled = false;
        }

        hands = FindObjectsByType<Hand>(FindObjectsSortMode.None);
        handWasEnabled = new bool[hands.Length];

        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] == null)
            {
                continue;
            }

            handWasEnabled[i] = hands[i].enabled;
            hands[i].enabled = false;
        }
    }

    public void EnableGameplayInput()
    {
        if (players != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                {
                    players[i].enabled = playerWasEnabled[i];
                }
            }
        }

        if (hands != null)
        {
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i] != null)
                {
                    hands[i].enabled = handWasEnabled[i];
                }
            }
        }
    }
}
