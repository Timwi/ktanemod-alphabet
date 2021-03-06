﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class AlphabetModule : MonoBehaviour
{

    public Material LED_On;
    public Material LED_Off;
    public MeshRenderer[] LEDs;
    static string[] WordBank = {
        "JQXZ",
        "PQJS",
        "OKBV",
        "QYDX",
        "IRNM",
        "ARGF",
        "LXE",
        "QEW",
        "TJL",
        "VCN",
        "HDU",
        "PKD",
        "VSI",
        "DFW",
        "ZNY",
        "YKQ",
        "GS",
        "AC",
        "JR",
        "OP"
    };
    bool activated = false;
    bool solved = false;

    string ButtonLabels = ""; // the labels of the buttons
    string CorrectCode = ""; // the correct code
    string TypingCode = ""; // the code that we're typing
    readonly bool[] LockedButtons = new bool[4]; // when we press a correct button, lock it

    static int _moduleIdCounter = 1;
    int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;

        ButtonLabels = GenerateButtons();
        CorrectCode = GenerateCorrectCode();
        KMSelectable[] buttons = GetComponent<KMSelectable>().Children;
        for (int i = 0; i < 4; i++)
        {
            SetLEDColor(i, false);
            buttons[i].GetComponentInChildren<TextMesh>().text = ButtonLabels.Substring(i, 1);
            int j = i;
            buttons[i].OnInteract += delegate ()
            {
                buttons[j].GetComponentInChildren<Animator>().SetTrigger("PushTrigger");
                OnPress(j);
                return false;
            };
        }
    }

    void SetLEDColor(int button, bool On)
    {
        MeshRenderer renderer = LEDs[button];
        renderer.material = On ? LED_On : LED_Off;
    }
    string GetNextCodeChar()
    {
        return CorrectCode.Substring(TypingCode.Length, 1);
    }
    bool IsCorrectButton(int button)
    {
        string Pressed = ButtonLabels.Substring(button, 1);
        string Next = GetNextCodeChar();
        return Pressed == Next;
    }

    void OnPress(int button)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (activated)
        {
            if (LockedButtons[button])
                return;

            Debug.LogFormat(@"[Alphabet #{0}] You pressed {1}, which is {2}correct{3}", _moduleId, ButtonLabels.Substring(button, 1), IsCorrectButton(button) ? "" : "NOT ", IsCorrectButton(button) ? "." : " — strike!");

            // not locked, so anything can happen now
            if (IsCorrectButton(button))
            {
                // yaay
                SetLEDColor(button, true);
                LockedButtons[button] = true;
                TypingCode += GetNextCodeChar();
                if (TypingCode.Length == 4)
                {
                    GetComponent<KMBombModule>().HandlePass();
                    activated = false;
                    solved = true;
                    Debug.LogFormat(@"[Alphabet #{0}] Module solved.", _moduleId);
                }
            }
            else
            {
                // strike
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }


    string CanSpell(string query, string with) // Query is the thing you're searching for, With is the string you're checking whether it can spell Query or not
    {
        int can = 0; // tells how many characters we could spell

        string newWith = ""; // this stores the unused characters

        for (int i = with.Length - 1; i >= 0; i--) // start from the end so removing doesn't fuck up stuff if we went up
        {
            string search = query.Substring(0, Mathf.Min(query.Length, with.Length)); // the portion of the string we're searching for
            string searchQuery = with.Substring(i, 1); // the search query

            if (search.Contains(searchQuery))
            {
                // we can spell this character, so count it
                can++;
            }
            else
            {
                newWith += with.Substring(i, 1); // otherwise, this is an unusable character, so it's a 'leftover'
            }
        }
        return can >= query.Length ? newWith : with; // if we managed to spell enough characters, return the leftovers, otherwise return the original With variable
    }

    string GenerateCorrectCode()
    {
        // first, we want to split the 4-worders, 3-worders, and 2-worders into their own lists so we can sort them individually
        // the ruleset states that the longest words are first, and ties are disputed by alphabetical order
        List<string> Words4 = new List<string>();
        List<string> Words3 = new List<string>();
        List<string> Words2 = new List<string>();
        foreach (string Data in WordBank)
        {
            if (Data.Length == 4)
                Words4.Add(Data);
        }
        foreach (string Data in WordBank)
        {
            if (Data.Length == 3)
                Words3.Add(Data);
        }
        foreach (string Data in WordBank)
        {
            if (Data.Length == 2)
                Words2.Add(Data);
        }
        Words4.Sort();
        Words3.Sort();
        Words2.Sort();

        // put it all into one big list
        List<string> All = Words4;
        All.AddRange(Words3);
        All.AddRange(Words2);

        string Code = ""; // the current code we have
        string Labels = ButtonLabels; // this will contain the 'leftovers' at the end, so store it locally in this scope

        for (int i = 0; i < All.Count; i++)
        {
            string remainders = CanSpell(All[i], Labels); // CanSpell will return the characters that weren't used to spell a word. if the word can't be spelled, it will return what was given as the 2nd argument
            if (remainders != Labels) // if the remainders have changed
            {
                Labels = remainders; // update them
                Debug.LogFormat(@"[Alphabet #{0}] {1}, you can spell {2}.", _moduleId, Code.Length > 0 ? "With the remaining letters" : "With these letters", All[i]);
                Code += All[i].ToUpperInvariant(); // append this word to the code
            }
            if (Code.Length >= 3) // if we're 3 or more characters, there's no more room for another word
                break;
        }

        // finally, sort the remainders by putting them into a list and taking them back out
        List<string> Remainders = new List<string>();
        for (int i = 0; i < Labels.Length; i++)
        {
            Remainders.Add(Labels.Substring(i, 1));
        }
        Remainders.Sort();
        Labels = "";
        for (int i = 0; i < Remainders.Count; i++)
        {
            Labels += Remainders[i];
        }

        Debug.LogFormat(@"[Alphabet #{0}] Letters to press are: {1}", _moduleId, Code + Labels);
        return Code + Labels; // the final code is the words spelled plus the alphabetically sorted leftover letters
    }

    void Activate()
    {
        activated = true;
    }

    string GenerateButtons()
    {
        // let's guarantee they can spell at least one word
        string toRet = WordBank[Random.Range(0, WordBank.Length)];
        // now let's fill in the rest

        while (toRet.Length < 4)
        {
            char chr = (char) Random.Range(65, 65 + 26);
            string Char = chr.ToString();
            if (!toRet.Contains(Char))
            {
                toRet += Char;
            }
        }

        // scramble it up just in case
        string newLetters = "";
        while (toRet.Length > 0)
        {
            int i = Random.Range(0, toRet.Length);
            newLetters += toRet.Substring(i, 1);
            toRet = toRet.Remove(i, 1);
        }

        Debug.LogFormat(@"[Alphabet #{0}] Letters on buttons are: {1}", _moduleId, newLetters);
        return newLetters;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Submit your answer with “!{0} press A B C D”.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (solved)
            yield break;

        var pieces = command.ToLowerInvariant().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (pieces.Length < 2 || (pieces[0] != "submit" && pieces[0] != "press"))
            yield break;

        var buttons = GetComponent<KMSelectable>().Children;
        var buttonLabels = string.Join("", buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLower()).ToArray());
        var buttonIndexes = pieces[1].Where(x => !char.IsWhiteSpace(x)).Select(x => buttonLabels.IndexOf(x)).ToList();
        if (buttonIndexes.Any(x => x < 0))
            yield break;

        yield return null;

        foreach (int ix in buttonIndexes)
        {
            buttons[ix].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!solved)
        {
            var valid = Enumerable.Range(0, 4).First(i => IsCorrectButton(i));
            GetComponent<KMSelectable>().Children[valid].OnInteract();
            yield return new WaitForSeconds(.25f);
        }
    }
}
