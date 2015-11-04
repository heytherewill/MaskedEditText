namespace MaskedEditText
{
    internal class Range
    {
        #region Constructors and Destructors
        
        internal Range()
        {
            Start = -1;
            End = -1;
        }

        #endregion

        #region Public Properties

        public int Start { get; set; }

        public int End { get; set; }
        
        #endregion
    }
}