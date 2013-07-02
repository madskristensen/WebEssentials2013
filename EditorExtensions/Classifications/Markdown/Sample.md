#Complex Markdown Sample
Use this file to test the Markdown syntax highlighter.

#Normal header!
[A link](http://blog.slaks.net/#csharp) is very nice.  
So are #hashtags.
Here is code with underscores: `abc + _something_ + def`, ending later with `_private`.  Operators are also a problem: `a * b` has a `*`, or `a **bold code** word`.

However, single ` characters should not be affected, even if there is **bold** afterwards.  

`code`, then _italic_, or **bold**.
Even  **_bold/italic_** or _**italic/bold**_

    No **code-bold** in _code blocks_

```cs
var str = @"**Nor** in fenced code blocks
#Not a header";
```

###Here's yet another header

> This is the first level of quoting.
>
> > This is a nested blockquote.
> > Note that snake_case_is_not_italic! 
>
> Back to the first level.
>   Three spaces _are not_ code
With continued lines

## Second header

> ## This is a header.
> 
> 1.   This is the first _list_ item.
> 2.   This is the second list item.
> 
> Here's some example code:
>    return shell _exec("echo $input | $markdown_ script");
>    string s = "Nested `code in code block`!";

> Here's a nested fence:
> > ```
This is _code_
```
