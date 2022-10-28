# Ream

<img src="img/Ream.png" style="zoom:10%;" />

## Overview

The programming language that uses common sense to prevent you from typing more than you need, simple yet powerful.

Check how to contribute and check changelog [here](Development.md)

You can join the official Discord [here](https://discord.gg/ExAfTcf8Nt)

The GitHub is [here](https://github.com/Melodi17/ream)



### Getting Started

#### Hello world

*Hello world* in Ream is identical to a few widely-used programming languages (e.g. Lua and Python).

```ream
print('Hello world!')
```

You can run this from a file by running it as follows `ream hello_world.r`, or you can run it through the interactive mode (REPL) by typing `ream` and entering `print('Hello world!')`. If everything is set up correctly, you should see `Hello world!` printed back to the screen.



### Fundamentals

#### Execution

Code in Ream is executed line-by-line and uses newline characters to determine how the code should be parsed. You can override this using the `;` and `\` characters. The semicolon inserts a newline to be read by the parser, whereas the backslash character suppresses the next character (if it's a newline).

The REPL can detect unfinished expressions and statements, and will reply with a continuation prompt `. ` (with the current indentation level) until it is complete. This enables the ability to write functions, classes and other multiline blocks within the REPL (although this can be done using the newline character even though it is harder to read)

Ream is designed to return `null` when something fails or is invalid, e.g. `a.b == null` will return `true` if `a` hasn't been declared and doesn't have a property of `b`. This behavior can be altered by setting `Flags.Strict = true`, but note that this will only effect some of these scenarios, it will not effect retrieving an unassigned variable, but it will effect attempting to call that invalid variable (since it will be `null`). It will also effect whether the `import` statement should throw an error in the case of the file not being found.



#### Types

Ream uses dynamic typing to store data, which allows for flexibility within the language. There are 9 primitive types, `null`, `Boolean`, `Number`, `String`, `Sequence`, `Dictionary`, `Prototype`, `Pointer` and `Callable`. 





### Elaboration



### Conventions



### Quirks
