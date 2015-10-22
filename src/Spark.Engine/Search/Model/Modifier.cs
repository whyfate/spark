﻿using Hl7.Fhir.Model;
using Spark.Search.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spark.Engine.Search.Model
{
    public enum Modifier
    {
        UNKNOWN = 0,
        EXACT = 1,
        PARTIAL = 2,
        TEXT = 3,
        CONTAINS = 4,
        ANYNAMESPACE = 5,
        MISSING = 6,
        BELOW = 7,
        ABOVE = 8,
        IN = 9,
        NOT_IN = 10,
        TYPE = 11,
        NONE = 12
    }

    public class ActualModifier
    {
        public string RawModifier { get; set; }

        public Type ModifierType { get; set; }

        public Modifier Modifier { get; set; }

        public const string MISSINGTRUE = "true";
        public const string MISSINGFALSE = "false";
        public const string MISSING_SEPARATOR = "=";

        private static Dictionary<string, Modifier> mapping = new Dictionary<string, Modifier>
            { {"exact", Modifier.EXACT }
            , {"partial", Modifier.PARTIAL }
            , {"text", Modifier.TEXT}
            , {"contains", Modifier.CONTAINS}
            , {"anyns", Modifier.ANYNAMESPACE }
            , {"missing", Modifier.MISSING }
            , {"below", Modifier.BELOW }
            , {"above", Modifier.ABOVE }
            , {"in", Modifier.IN }
            , {"not-in", Modifier.NOT_IN }
            , {"", Modifier.NONE } };

        public ActualModifier(string rawModifier)
        {
            RawModifier = rawModifier;
            Missing = TryParseMissing(rawModifier);
            if (Missing.HasValue)
            {
                Modifier = Modifier.MISSING;
                return;
            }
            ModifierType = TryGetType(rawModifier);
            if (ModifierType != null)
            {
                Modifier = Modifier.TYPE;
                return;
            }
            Modifier = mapping.FirstOrDefault(m => m.Key.Equals(rawModifier, StringComparison.InvariantCultureIgnoreCase)).Value;
        }

        public bool? Missing { get; set; }

        /// <summary>
        /// Catches missing, missing=true and missing=false
        /// </summary>
        /// <param name="rawModifier"></param>
        /// <returns></returns>
        private bool? TryParseMissing(string rawModifier)
        {
            string missing = mapping.FirstOrDefault(m => m.Value == Modifier.MISSING).Key;
            string[] parts = rawModifier.Split(new string[] { MISSING_SEPARATOR }, StringSplitOptions.None);
            if (parts[0].Equals(missing, StringComparison.InvariantCultureIgnoreCase))
            {
                if (parts.Length > 1)
                {
                    if (parts[1].Equals(MISSINGTRUE, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else if (parts[1].Equals(MISSINGFALSE, StringComparison.InvariantCultureIgnoreCase))
                        return false;
                    else
                        throw Error.Argument("rawModifier", "For the :missing modifier, only values '{0}' and '{1}' are allowed", MISSINGTRUE, MISSINGFALSE);
                }
                return true;
            }
            return null;
        }

        private static Regex typeModifierPatter = new Regex(@"\[(.*)\]");  
        private Type TryGetType(string rawModifier)
        {
            Type result = null;
            var match = typeModifierPatter.Match(rawModifier);
            if (match.Success)
            {
                var typeName = match.Groups[1].Value; //Groups[0] is by default the whole match.
                result = ModelInfo.GetTypeForResourceName(typeName);
                if (result == null)
                    throw Error.Argument("rawModifier", "For the :[type] modifier, {0} is not a valid type.", typeName);
            }
            return result;
        }

        public override string ToString()
        {
            string modifierText = mapping.FirstOrDefault(m => m.Value == Modifier).Key;
            switch (Modifier)
            {
                case Modifier.MISSING:
                    {
                        return modifierText + MISSING_SEPARATOR + (Missing.Value ? MISSINGTRUE : MISSINGFALSE);
                    }
                case Modifier.TYPE:
                    {
                        return String.Format("[{0}]", ModelInfo.GetResourceNameForType(ModifierType));
                    }
                default: return modifierText;
            }
        }

    }
}
