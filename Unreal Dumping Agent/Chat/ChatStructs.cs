﻿using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ML.Data;

namespace Unreal_Dumping_Agent.Chat
{
    public static class ChatStructs
    {
        // Keys must be lower
        public static readonly Dictionary<string, EQuestionTask> ChatTasks = new Dictionary<string, EQuestionTask>
        {
            { "gname", EQuestionTask.GNames },
            { "gnames", EQuestionTask.GNames },
            { "names", EQuestionTask.GNames },

            { "gobject", EQuestionTask.GObject },
            { "gobjects", EQuestionTask.GObject },
            { "objects", EQuestionTask.GObject },

            { "process", EQuestionTask.Process },
            { "target", EQuestionTask.Process },

            { "sdk", EQuestionTask.Sdk },
            { "tool", EQuestionTask.Tool },
        };
    }
    

    public enum EQuestionType
    {
        None = 0,
        HiWelcome = 1,
        Thanks = 2,
        LifeAsk = 3,
        Ask = 4,
        Funny = 5,
        Open = 6,
        Help = 7,
        Set = 8,

        Find = 101,
        LockProcess = 102,
        SdkDump = 103
    }
    public enum EQuestionTask
    {
        None = 0,
        GNames = 1,
        GObject = 2,
        Process = 3,
        Sdk = 4,
        Tool = 5
    }

    internal class ChatQuestion
    {
        public string QuestionText { get; set; }
        public int QuestionType { get; set; } // EQuestionType
        public int QuestionTask { get; set; } // EQuestionTask
    }
    [DebuggerDisplay("Type = {TypeEnum()}, Task = {TaskEnum()}")]
    public class QuestionPrediction
    {
        [ColumnName("PredictedType")]
        public int QuestionType;

        public int QuestionTask;

        public EQuestionType TypeEnum() => (EQuestionType)QuestionType;
        public EQuestionTask TaskEnum() => (EQuestionTask)QuestionTask;
    }
}
