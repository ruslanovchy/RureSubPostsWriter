using System.Text.RegularExpressions;

namespace RureSubPostsWriter;

public static partial class PostsWriterValidator
{
    public const string TITLE_REGEX = "^(?!.*(<script|javascript:|on\\w+=|<iframe|<img|<a\\s))[^\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F\\x7F]{0,400}$";
    public const string TEXT_REGEX = "^(?!.*(<script|javascript:|on\\w+=|<iframe|<img|<a\\s))[^\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F\\x7F]{0,1000}$";
}
