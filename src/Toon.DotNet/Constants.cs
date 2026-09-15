namespace ToonFormat;

/// <summary>
/// Constants used in the TOON format specification.
/// </summary>
public static class Constants
{
    #region Spec version

    /// <summary>
    /// The TOON format specification version this library conforms to.
    /// See https://github.com/toon-format/spec/releases/tag/v4.1.1 and
    /// TOON_V4.md for the v4.0.0-v4.1.1 gap list and compliance status
    /// that established this baseline (superseding the v3.3.2 baseline
    /// TOON_V3.md established). One known gap remains at this version:
    /// the decoder's number tokenization has not been fully audited
    /// against spec §4's normative number grammar, and out-of-range
    /// numeric literal handling is unverified — tracked in TOON_V4.md's
    /// "Number grammar" gap-list row. Update this when that gap closes
    /// or the implementation moves to a newer spec version.
    /// </summary>
    public const string SpecVersion = "4.1.1";

    #endregion

    #region List markers
    
    /// <summary>
    /// The character used to mark list items.
    /// </summary>
    public const char ListItemMarker = '-';
    
    /// <summary>
    /// The prefix used for list items (marker + space).
    /// </summary>
    public const string ListItemPrefix = "- ";
    
    #endregion

    #region Structural characters
    
    /// <summary>
    /// Comma delimiter character.
    /// </summary>
    public const char Comma = ',';
    
    /// <summary>
    /// Colon separator character.
    /// </summary>
    public const char Colon = ':';
    
    /// <summary>
    /// Space character.
    /// </summary>
    public const char Space = ' ';
    
    /// <summary>
    /// Pipe delimiter character.
    /// </summary>
    public const char Pipe = '|';
    
    /// <summary>
    /// Hash/pound character used for length markers.
    /// </summary>
    public const char Hash = '#';
    
    #endregion

    #region Brackets and braces
    
    /// <summary>
    /// Opening square bracket.
    /// </summary>
    public const char OpenBracket = '[';
    
    /// <summary>
    /// Closing square bracket.
    /// </summary>
    public const char CloseBracket = ']';
    
    /// <summary>
    /// Opening curly brace.
    /// </summary>
    public const char OpenBrace = '{';
    
    /// <summary>
    /// Closing curly brace.
    /// </summary>
    public const char CloseBrace = '}';
    
    #endregion

    #region Literals
    
    /// <summary>
    /// The string representation of null values.
    /// </summary>
    public const string NullLiteral = "null";
    
    /// <summary>
    /// The string representation of true values.
    /// </summary>
    public const string TrueLiteral = "true";
    
    /// <summary>
    /// The string representation of false values.
    /// </summary>
    public const string FalseLiteral = "false";
    
    #endregion

    #region Escape characters
    
    /// <summary>
    /// Backslash character used for escaping.
    /// </summary>
    public const char Backslash = '\\';
    
    /// <summary>
    /// Double quote character.
    /// </summary>
    public const char DoubleQuote = '"';
    
    /// <summary>
    /// Newline character.
    /// </summary>
    public const char Newline = '\n';
    
    /// <summary>
    /// Carriage return character.
    /// </summary>
    public const char CarriageReturn = '\r';
    
    /// <summary>
    /// Tab character.
    /// </summary>
    public const char Tab = '\t';
    
    #endregion

    #region Delimiters
    
    /// <summary>
    /// Available delimiter characters for TOON formatting.
    /// </summary>
    public static class Delimiters
    {
        /// <summary>
        /// Comma delimiter.
        /// </summary>
        public const char Comma = Constants.Comma;
        
        /// <summary>
        /// Tab delimiter.
        /// </summary>
        public const char Tab = Constants.Tab;
        
        /// <summary>
        /// Pipe delimiter.
        /// </summary>
        public const char Pipe = Constants.Pipe;
    }
    
    /// <summary>
    /// The default delimiter used for TOON formatting.
    /// </summary>
    public const char DefaultDelimiter = Delimiters.Comma;
    
    #endregion
}