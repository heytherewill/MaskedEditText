using System;

namespace MaskedEditText
{
    internal class RawText
    {
        #region Fields

        private string _text = "";
        
        #endregion

        #region Public Properties

        internal string Text
        {
            get
            {
                return _text;
            }
        }

        internal int Length
        {
            get
            {
                return _text.Length;
            }
        }

        internal char this[int position]
        {
            get
            {
                return _text[position];
            }
        }

        #endregion

        #region Public Methods and Operators

        internal void SubtractFromString(Range range)
        {
            var firstPart = "";
            var lastPart = "";

            if (range.Start > 0 && range.Start <= _text.Length)
            {
                firstPart = _text.Substring(0, range.Start);
            }
            if (range.End >= 0 && range.End < _text.Length)
            {
                lastPart = _text.Substring(range.End, _text.Length);
            }
            _text = firstPart + lastPart;
        }

        internal int AddToString(string newString, int start, int maxLength)
        {
            var firstPart = "";
            var lastPart = "";

            if (newString == null || newString == "")
            {
                return 0;
            }
            else if (start < 0)
            {
                throw new ArgumentOutOfRangeException("Start position must be non-negative");
            }
            else if (start > _text.Length)
            {
                throw new ArgumentOutOfRangeException("Start position must be less than the actual text length");
            }

            int count = newString.Length;

            if (start > 0)
            {
                firstPart = _text.Substring(0, start);
            }
            if (start >= 0 && start < _text.Length)
            {
                lastPart = _text.Substring(start, _text.Length);
            }
            if (_text.Length + newString.Length > maxLength)
            {
                count = maxLength - _text.Length;
                newString = newString.Substring(0, count);
            }
            _text = firstPart + newString + lastPart;
            return count;
        }

        #endregion
    }
}