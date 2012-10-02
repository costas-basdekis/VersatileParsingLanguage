# Regular Expressions augmented with variables, control flow and functions

An attempt to create a non-verbose language to automate parsing
from text (and in a more cubersome way binaries) using regular expressions
with the addition of variables, control flow and functions. It is most usefull
in cases where multiple raw or logging formats can be converted to the same
target format. The target format supported is a JSON like tree structure
with key->value as it's elements.

## An editor, compiler and debugger

The compiler produces an internal "byte-code" which is implemented using classes
instead of merely byte-code. The editor highlights the source code in real-time,
and supports including external files. The dubugger supportes step-by-step or by
breakpoints execution inspection of stack, variables and result. Unfortunately,
breakpoints are not preserved if source code changes (which would require some
engineering to be accomplished), and variables cannot be changed (which would 
require rather easy to implement UI changes).
