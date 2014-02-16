/*
Welcome to sweet.js! 

You can play around with macro writing here on the left side and 
your code will automatically be compiled on the right. This page 
will also save your code to localStorage on every successful 
compile so feel free to close the page and come back later!
*/

// Here is a really simple identity macro to get started. 

// The `macro` keyword is used to create and name new macros.
macro id { 
  rule {
    // after the macro name, match:
    // (1) a open paren 
    // (2) a single token and bind it to `$x`
    // (3) a close paren
    ($x)
  } => {
    // just return the token we bound to `$x`
    $x
  }
}
id ( 42 );

// Note that a single token to sweet.js includes matched 
// delimiters not just numbers and identifiers. For example,
// an array with all of its elements counts as one token:
id ([1,2,3])

// One of the really important things sweet.js does is protect
// macros from unintentionally binding or capturing variables they
// weren't supposed to. This is called hygiene and to enforce hygiene 
// sweet.js must carefully rename all variable names.

var x; // note the different name on the right --->

// For example, let's look at the swap macro that uses a temporary 
// variable to swap the contents of two other vars:
macro swap {
  rule {
    ($x, $y)
  } => {
    var tmp = $y;
    $y = $x;
    $x = tmp;
  }
}

var foo = 100;
var bar = 200;
var tmp = 'my other temporary variable';

swap (foo, bar)

// Notice that even though the `tmp` variable we made outside of
// the macro had the same name as the `tmp` variable inside the
// macro, sweet.js kept them distinct.


