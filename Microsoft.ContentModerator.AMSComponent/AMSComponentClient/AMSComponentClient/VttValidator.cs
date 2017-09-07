using System.IO;
using Jint;
using Jint.Native;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    public class VttValidator
    {
        private readonly static Engine _engine = new Engine();
        private readonly static JsValue _vttValidator;
        private readonly static string _option = @"subtitles / captions / descriptions";
        private readonly static string VttValidatorPath = @"..\Lib\WebVTTValidator.js";

        static VttValidator()
        {
            string source = File.ReadAllText(VttValidatorPath);
            _vttValidator = _engine.GetValue(_engine.Execute(source)
                .Execute("var parser = new WebVTTParser();")
                .GetValue("parser"), "parse");
        }

        internal static double ValidateVTT(string vtt)
        {
            return _vttValidator.Invoke(vtt, _option).AsObject()
                .GetOwnProperty("errors").Value.AsArray().GetOwnProperty("length").Value.AsNumber();
        }
    }
}
