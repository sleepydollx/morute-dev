using UnityEngine;
public static class Chapter1Dialogues
{
    public static DialogueSequence WakeUp()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_wakeup";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Diana",
                text = "Irma?"
            },
            new DialogueLine
            {
                SpeakerName = "Diana",
                text = "What the hell you're doing here?"
            },
            new DialogueLine
            {
                SpeakerName = "Diana",
                text = "You can always talk to me, you know?"
            },
            new DialogueLine
            {
                SpeakerName = "Irma",
                text = "I don't need your fake ass sympathy."
            },
            new DialogueLine
            {
                SpeakerName = "Diana",
                text = "I know. But you have to be careful. This place isn't safe."
            },
            new DialogueLine
            {
                SpeakerName = "Diana",
                text = "There are things in this house. Things that want to hurt you."
            }
        };
        return seq;
    }


    public static DialogueSequence EnterHallway()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_hallway";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Sherry",
                text = "Return to the shore, no mans can shield."
            },
            new DialogueLine
            {
                SpeakerName = "Sherry",
                text = "In The Name of The Father, The Son, and The Holy Mary."
            },
            new DialogueLine
            {
                SpeakerName = "Janet",
                text = "The fuck?"
            },
        };
        return seq;
    }


    public static DialogueSequence BackyardDoor()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_locked_door";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "Locked. Of course it's locked."
            },
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "There has to be a key somewhere in this house."
            },
        };
        return seq;
    }


    public static DialogueSequence HearsSoundNoSource()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_sound";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "What was that?"
                //      ↑ CHANGE THIS
            },
            new DialogueLine
            {
                SpeakerName = "",
                text = "A sound — like something heavy dragging across the floor above you."
                //      ↑ CHANGE THIS
            },
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "I'm not alone in here."
                //      ↑ CHANGE THIS
            },
        };
        return seq;
    }


    public static DialogueSequence FoundKey()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_key_found";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "A key. Old and rusted — but it might work."
                //      ↑ CHANGE THIS
            },
        };
        return seq;
    }

    // ── CHAPTER 1 ENDING ─────────────────────────────────────────────────────

    public static DialogueSequence Chapter1End()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch1_end";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Janet",
                text = "The door swings open. Cold air pours out from below."
                //      ↑ CHANGE THIS
            },
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "The basement. I have to go down."
                //      ↑ CHANGE THIS
            },
            new DialogueLine
            {
                SpeakerName = "",
                text = "Something tells you this is the point of no return. Once you step down there, there's no going back."
            },
        };
        return seq;
    }
}

public static class Chapter2Dialogues
{
    // ── CHAPTER 2 START ─────────────────────────────────────────────────────

    public static DialogueSequence Chapter2Start()
    {
        var seq = ScriptableObject.CreateInstance<DialogueSequence>();
        seq.sequenceId = "ch2_start";
        seq.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "The basement is dark and cold. The air is thick with dust and mold."
                //      ↑ CHANGE THIS
            },
            new DialogueLine
            {
                SpeakerName = "Player",
                text = "I can barely see anything down here."
                //      ↑ CHANGE THIS
            },
        };
        return seq;
    }
}