//*********************************************************************
//  Dependencies: System
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public static class REST_Logging
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Enums
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public enum Colors : ushort
        {
            White,
            Red,
            Green,
            Blue,
            Yellow,

            Fatal,
            MSG,
        };


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Enums
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly static string s_ColorCode_White = "#FFFFFF";
        private readonly static string s_ColorCode_Red = "#FF0000";
        private readonly static string s_ColorCode_Green = "#00FF00";
        private readonly static string s_ColorCode_Blue = "#0000FF";
        private readonly static string s_ColorCode_Yellow = "#FFFF00";

        private readonly static string s_ColorCode_Fatal = "#420303";
        private readonly static string s_ColorCode_MSG = "#C0C0C0";

        private readonly static string TextFormat_Coloring= "<color={0}>{1}</color>";

        private static string PREFIX_INFO     = ColorizeString("[  INFO  ]", Colors.Green);
        private static string PREFIX_WARRNING = ColorizeString("[  WARR  ]", Colors.Yellow);
        private static string PREFIX_ERROR    = ColorizeString("[  ERROR  ]", Colors.Red);
        private static string PREFIX_FATAL    = ColorizeString("[  FATAL  ]", Colors.Fatal);
        private static string PREFIX_DEBUG    = ColorizeString("[  DEBUG  ]", Colors.White);

        private static string MESSAGE_FORMAT = "{0} <color=#C0C0C0>{1} : {2}</color> | {3}";
        private static string MESSAGE_FORMAT_NOMSG = "{0} <color=#C0C0C0>{1} : {2}</color>";
        private static string MESSAGE_FORMAT_VALUE = "{0} <color=#C0C0C0>{1} : {2}</color> | {3} {4}";
        private static string MESSAGE_FORMAT_ONLYMSG = "{0} {1}";

        public readonly static string s_Literal_Ignored = ColorizeString("(Ignored)", Colors.Yellow);
        public readonly static string s_Literal_Null    = ColorizeString("null", Colors.Red);
        public readonly static string s_Literal_True    = ColorizeString("true", Colors.Green);
        public readonly static string s_Literal_False   = ColorizeString("false", Colors.Red);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region public logging methodes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Info
        public static void Info(in string p_ModuleName, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_NOMSG, PREFIX_INFO, p_ModuleName, p_FunctionName));
        }

        public static void Info(in string p_ModuleName, in string m_Message, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT, PREFIX_INFO, p_ModuleName, p_FunctionName, m_Message));
        }

        public static void Info(in string p_ModuleName, in string m_Message, in string m_MessageData, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_INFO, p_ModuleName, p_FunctionName, m_Message, m_MessageData));
        }
        #endregion //Info

        //--------------------------------------------------------------------------------------------------------------------
        #region Warrning
        public static void Warrning(in string p_ModuleName, in string p_FunctionName)
        {
            UnityEngine.Debug.LogWarning(String.Format(MESSAGE_FORMAT_NOMSG, PREFIX_WARRNING, p_ModuleName, p_FunctionName));
        }
        public static void Warrning(in string p_ModuleName, in string p_FunctionName, in string m_Message)
        {
            UnityEngine.Debug.LogWarning(String.Format(MESSAGE_FORMAT, PREFIX_WARRNING, p_ModuleName, p_FunctionName, m_Message));
        }
        #endregion //Warrning

        //--------------------------------------------------------------------------------------------------------------------
        #region Error
        public static void Error(in string p_ModuleName, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT_NOMSG, PREFIX_ERROR, p_ModuleName, p_FunctionName));
        }
        public static void Error(in string p_ModuleName, in string m_Message, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT, PREFIX_ERROR, p_ModuleName, p_FunctionName, m_Message));
        }
        public static void Error<T>(in string p_ModuleName, in string m_Message, in T p_Data, [CallerMemberName] string p_FunctionName = "") where T : IFormattable
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_ERROR, p_ModuleName, p_FunctionName, m_Message, p_Data.ToString()));
        }
        public static void Error(in string p_ModuleName, in string m_Message, in string p_StringData, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_ERROR, p_ModuleName, p_FunctionName, m_Message, p_StringData));
        }
        #endregion //Error

        //--------------------------------------------------------------------------------------------------------------------
        #region Fatal
        public static void Fatal(in string p_ModuleName, in string m_Message, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT, PREFIX_FATAL, p_ModuleName, p_FunctionName, m_Message));
            QuitOrStopGame();
        }
        public static void Fatal(in string p_ModuleName, in string m_Message, in string p_StringData, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_FATAL, p_ModuleName, p_FunctionName, m_Message, p_StringData));
            QuitOrStopGame();
        }
        public static void Fatal<T>(in string p_ModuleName, in string m_Message, in T p_Data, [CallerMemberName] string p_FunctionName = "") where T : IFormattable
        {
            UnityEngine.Debug.LogError(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_FATAL, p_ModuleName, p_FunctionName, m_Message, p_Data));
            QuitOrStopGame();
        }
        #endregion //Fatal

        //--------------------------------------------------------------------------------------------------------------------
        #region Debug
        public static void Debug<T>(in bool p_Condition, in T m_Message)
        {
            if(p_Condition) UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_ONLYMSG, PREFIX_DEBUG, m_Message));
        }
        public static void Debug<T>(in T m_Message)
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_ONLYMSG, PREFIX_DEBUG, m_Message));
        }
        public static void Debug(in string p_ModuleName, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_NOMSG, PREFIX_DEBUG, p_ModuleName, p_FunctionName));
        }
        public static void Debug(in string p_ModuleName, in string m_Message, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT, PREFIX_DEBUG, p_ModuleName, p_FunctionName, m_Message));
        }
        public static void Debug(in string p_ModuleName, in string m_Message, in string m_MessageData, [CallerMemberName] string p_FunctionName = "")
        {
            UnityEngine.Debug.Log(String.Format(MESSAGE_FORMAT_VALUE, PREFIX_DEBUG, p_ModuleName, p_FunctionName, m_Message, m_MessageData));
        }
        #endregion //Debug

        private static void QuitOrStopGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // public logging methodes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Data Manipulation methodess
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBinaryString(int number)
        {
            const int size = 32;

            char[] binaryString = new char[size];
            for (int i = 0; i < size; ++i)
            {
                binaryString[size - 1 - i] = ((number & (1 << i)) > 0) ? '1' : '0';
            }
            return new string(binaryString);
        }

        public static string GetBinaryString(ulong number)
        {
            const int size = 64;

            char[] binaryString = new char[size];
            for (int i = 0; i < size; ++i)
            {
                binaryString[size - 1 - i] = ((number & (((ulong)1) << i)) > 0) ? '1' : '0';
            }
            return new string(binaryString);
        }

        public static string GetBinaryString(long number)
        {
            const int size = 64;

            char[] binaryString = new char[size];
            for (int i = 0; i < size; ++i)
            {
                binaryString[size - 1 - i] = ((number & (((long)1) << i)) > 0) ? '1' : '0';
            }
            return new string(binaryString);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Data Manipulation methodess
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Colorize String
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public static string ColorizeString(string p_Input, Colors p_Color)
        {
            switch (p_Color)
            {
                case Colors.Green: return ColorizeStringImpl(p_Input, s_ColorCode_Green);
                case Colors.Red: return ColorizeStringImpl(p_Input, s_ColorCode_Red);
                case Colors.Blue: return ColorizeStringImpl(p_Input, s_ColorCode_Blue);
                case Colors.Yellow: return ColorizeStringImpl(p_Input, s_ColorCode_Yellow);
                case Colors.Fatal: return ColorizeStringImpl(p_Input, s_ColorCode_Fatal);
                case Colors.MSG: return ColorizeStringImpl(p_Input, s_ColorCode_MSG);
                case Colors.White: 
                default:
                    return  ColorizeStringImpl(p_Input, s_ColorCode_White);
            }
        }

        private static string ColorizeStringImpl(in string p_Msg, in string p_Color)
        {
            return String.Format(TextFormat_Coloring, p_Color, p_Msg);
        }

        public static string ColorizeBlooean (in bool p_Value)
        {
            return p_Value ? s_Literal_True : s_Literal_False;
        }

        public static string ColorizeNumberHigherThanZero(in int p_Value)
        {
            return ColorizeStringImpl(p_Value.ToString(), p_Value > 0 ? s_ColorCode_Green : s_ColorCode_Red);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Colorize String
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}