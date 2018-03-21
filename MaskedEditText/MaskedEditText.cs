using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using System;

namespace MaskedEditText
{
    public class MaskedEditText : EditText, View.IOnFocusChangeListener, ITextWatcher, TextView.IOnEditorActionListener
    {
        #region Constructors and Destructors

        public MaskedEditText(Context context)
            : base(context)
        {
        }

        public MaskedEditText(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(attrs);
        }

        public MaskedEditText(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected MaskedEditText(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        #endregion

        #region Fields

        private int _selection;

        private int _maxRawLength;

        private int _lastValidMaskPosition;

        private char _representation;

        private char _maskFill;

        private bool _ignore;

        private bool _initialized;

        private bool _editingAfter;

        private bool _editingBefore;

        private bool _editingOnChanged;

        private bool _selectionChanged;

        private int[] _rawToMask;

        private int[] _maskToRaw;

        private char[] _charsInMask;

        private string _mask;

        private RawText _rawText = new RawText();

        private IOnFocusChangeListener _focusChangeListener;

        #endregion

        #region Public Properties
        
        public string Mask
        {
            get
            {
                return _mask;
            }
            set
            {
                _mask = value;
                CleanUp();
            }
        }

        private char Representation
        {
            get
            {
                return _representation;
            }
            set
            {
                _representation = value;
                CleanUp();
            }
        }

        public override IOnFocusChangeListener OnFocusChangeListener
        {
            get
            {
                return base.OnFocusChangeListener;
            }

            set
            {
                _focusChangeListener = value;
            }
        }

        #endregion

        #region Public Methods and Operators
        
        public void OnFocusChange(View v, bool hasFocus)
        {
            if (_focusChangeListener != null)
            {
                _focusChangeListener.OnFocusChange(v, hasFocus);
            }

            if (HasFocus && (_rawText.Length > 0 || !HasHint))
            {
                _selectionChanged = false;
                SetSelection(LastValidPosition());
            }
        }

        public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
        {
            switch (actionId)
            {
                case ImeAction.Next:
                    return false;
                default:
                    return true;
            }
        }

        #endregion

        #region Properties

        private bool HasHint
        {
            get
            {
                return Hint != null;

            }
        }

        #endregion

        #region Methods

        private void Initialize(IAttributeSet attrs)
        {
            var styledAttributes = Context.ObtainStyledAttributes(attrs, ru.plusofonxamarin.Resource.Styleable.MaskedEditText);
            var count = styledAttributes.IndexCount;

            for (var i = 0; i < count; ++i)
            {
                var attr = styledAttributes.GetIndex(i);
                if (attr == ru.plusofonxamarin.Resource.Styleable.MaskedEditText_Mask)
                {
                    AddTextChangedListener(this);

                    _mask = styledAttributes.GetString(attr);
                    _maskFill = (styledAttributes.GetString(ru.plusofonxamarin.Resource.Styleable.MaskedEditText_MaskFill) ?? " ")[0];
                    _representation = (styledAttributes.GetString(ru.plusofonxamarin.Resource.Styleable.MaskedEditText_CharRepresentation) ?? "#")[0];

                    CleanUp();

                    SetOnEditorActionListener(this);
                }
            }

            styledAttributes.Recycle();
        }

        private void CleanUp()
        {
            if (Mask != null)
            {
                _initialized = false;

                GeneratePositionArrays();

                _rawText = new RawText();
                _selection = _rawToMask[0];

                _editingBefore = true;
                _editingOnChanged = true;
                _editingAfter = true;

                Text = HasHint ? null : Mask.Replace(_representation, _maskFill); ;

                _editingBefore = false;
                _editingOnChanged = false;
                _editingAfter = false;

                _maxRawLength = _maskToRaw[PreviousValidPosition(Mask.Length - 1)] + 1;
                _lastValidMaskPosition = FindLastValidMaskPosition();
                _initialized = true;

                base.OnFocusChangeListener = this;
            }
        }

        private int FindLastValidMaskPosition()
        {
            for (int i = _maskToRaw.Length - 1; i >= 0; i--)
            {
                if (_maskToRaw[i] != -1) return i;
            }

            throw new RuntimeException("Mask contains only the representation char");
        }

        private void GeneratePositionArrays()
        {
            var aux = new int[Mask.Length];
            _maskToRaw = new int[Mask.Length];
            var charsInMaskAux = "";

            int charIndex = 0;
            for (int i = 0; i < Mask.Length; i++)
            {
                char currentChar = Mask[i];
                if (currentChar == _representation)
                {
                    aux[charIndex] = i;
                    _maskToRaw[i] = charIndex++;
                }
                else
                {
                    var charAsString = currentChar.ToString();
                    if (!charsInMaskAux.Contains(charAsString) && !Character.IsLetter(currentChar) && !Character.IsDigit(currentChar))
                    {
                        charsInMaskAux = charsInMaskAux + charAsString;
                    }

                    _maskToRaw[i] = -1;
                }
            }

            if (charsInMaskAux.IndexOf(' ') < 0)
            {
                charsInMaskAux = charsInMaskAux + " ";
            }

            _charsInMask = charsInMaskAux.ToCharArray();

            _rawToMask = new int[charIndex];
            for (int i = 0; i < charIndex; i++)
            {
                _rawToMask[i] = aux[i];
            }
        }

        private int ErasingStart(int start)
        {
            while (start > 0 && _maskToRaw[start] == -1)
            {
                start--;
            }
            return start;
        }

        protected override void OnSelectionChanged(int selStart, int selEnd)
        {
            // On Android 4+ this method is being called more than 1 time if there is a hint in the EditText, what moves the cursor to left
            // Using the boolean var selectionChanged to limit to one execution
            if (Mask == null)
            {
                base.OnSelectionChanged(selStart, selEnd);
                return;
            }

            if (_initialized)
            {
                if (!_selectionChanged)
                {

                    if (_rawText.Length == 0 && HasHint)
                    {
                        selStart = 0;
                        selEnd = 0;
                    }
                    else
                    {
                        selStart = FixSelection(selStart);
                        selEnd = FixSelection(selEnd);
                    }
                    SetSelection(selStart, selEnd);
                    _selectionChanged = true;
                }
                else
                {
                    //check to see if the current selection is outside the already entered text
                    if (!(HasHint && _rawText.Length == 0) && selStart > _rawText.Length - 1)
                    {
                        SetSelection(FixSelection(selStart), FixSelection(selEnd));
                    }
                }
            }

            base.OnSelectionChanged(selStart, selEnd);
        }

        private int FixSelection(int selection)
        {
            var lastValidPosition = LastValidPosition();

            if (selection > lastValidPosition)
            {
                return lastValidPosition;
            }
            else
            {
                return NextValidPosition(selection);
            }
        }

        private int NextValidPosition(int currentPosition)
        {
            while (currentPosition < _lastValidMaskPosition && _maskToRaw[currentPosition] == -1)
            {
                currentPosition++;
            }
            if (currentPosition > _lastValidMaskPosition) return _lastValidMaskPosition + 1;
            return currentPosition;
        }

        private int PreviousValidPosition(int currentPosition)
        {
            while (currentPosition >= 0 && _maskToRaw[currentPosition] == -1)
            {
                currentPosition--;
                if (currentPosition < 0)
                {
                    return NextValidPosition(0);
                }
            }
            return currentPosition;
        }

        private int LastValidPosition()
        {
            if (_rawText.Length == _maxRawLength)
            {
                return _rawToMask[_rawText.Length - 1] + 1;
            }
            return NextValidPosition(_rawToMask[_rawText.Length]);
        }

        private string MakeMaskedText()
        {
            char[] maskedText = Mask.Replace(_representation, ' ').ToCharArray();
            for (int i = 0; i < _rawToMask.Length; i++)
            {
                if (i < _rawText.Length)
                {
                    maskedText[_rawToMask[i]] = _rawText[i];
                }
                else
                {
                    maskedText[_rawToMask[i]] = _maskFill;
                }
            }
            return new string(maskedText);
        }

        private Range calculateRange(int start, int end)
        {
            var range = new Range();
            for (int i = start; i <= end && i < Mask.Length; i++)
            {
                if (_maskToRaw[i] != -1)
                {
                    if (range.Start == -1)
                    {
                        range.Start = _maskToRaw[i];
                    }

                    range.End = _maskToRaw[i];
                }
            }
            if (end == Mask.Length)
            {
                range.End = _rawText.Length;
            }
            if (range.Start == range.End && start < end)
            {
                int newStart = PreviousValidPosition(range.Start - 1);
                if (newStart < range.Start)
                {
                    range.Start = newStart;
                }
            }
            return range;
        }

        private string Clear(string str)
        {
            foreach (var c in _charsInMask)
            {
                str = str.Replace(c.ToString(), "");
            }
            return str;
        }

        void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {
            if (Mask != null)
            {
                if (!_editingBefore)
                {
                    _editingBefore = true;

                    if (start > _lastValidMaskPosition)
                    {
                        _ignore = true;
                    }

                    int rangeStart = start;
                    if (after == 0)
                    {
                        rangeStart = ErasingStart(start);
                    }

                    Range range = calculateRange(rangeStart, start + count);
                    if (range.Start != -1)
                    {
                        _rawText.SubtractFromString(range);
                    }
                    if (count > 0)
                    {
                        _selection = PreviousValidPosition(start);
                    }
                }
            }
        }

        void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            if (Mask != null)
            {

                if (!_editingOnChanged && _editingBefore)
                {
                    _editingOnChanged = true;
                    if (_ignore)
                    {
                        return;
                    }
                    if (count > 0)
                    {
                        var startingPosition = _maskToRaw[NextValidPosition(start)];
                        var addedString = s.SubSequence(start, start + count).ToString();
                        count = _rawText.AddToString(Clear(addedString), startingPosition, _maxRawLength);
                        if (_initialized)
                        {
                            int currentPosition = startingPosition + count < _rawToMask.Length ?
                                                _rawToMask[startingPosition + count] :
                                                currentPosition = _lastValidMaskPosition + 1;

                            _selection = NextValidPosition(currentPosition);
                        }
                    }
                }
            }
        }

        void ITextWatcher.AfterTextChanged(IEditable s)
        {
            if (Mask != null)
            {

                if (!_editingAfter && _editingBefore && _editingOnChanged)
                {
                    _editingAfter = true;
                    if (_rawText.Length == 0 && HasHint)
                    {
                        _selection = 0;
                        Text = null;
                    }
                    else
                    {
                        Text = MakeMaskedText();
                    }

                    _selectionChanged = false;
                    SetSelection(_selection);

                    _editingBefore = false;
                    _editingOnChanged = false;
                    _editingAfter = false;
                    _ignore = false;
                }
            }
        }

        #endregion
    }
}