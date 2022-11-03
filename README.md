# Ream

<img src="img/Ream.png" style="zoom:10%;" />

## Overview

The programming language for our sanity. Simple, but powerful, the language offers a large range of features that enhance the development experience.

Check how to contribute and check changelog [here](Development.md)

You can join the official Discord [here](https://discord.gg/ExAfTcf8Nt)

The GitHub is [here](https://github.com/Melodi17/ream)



### Getting Started

#### Hello world

*Hello world* in Ream requires the IO library to output to the screen, this can be installed using the Box package manager or by manually downloading the [IO.dll](https://github.com/Melodi17/Ream/raw/main/Libraries/IO.dll) file and moving it into the same directory as the script.

```ream
import IO

IO.Println('Hello world!')
```

You can run this from a file by running it as follows `ream hello_world.r`, or you can run it through the interactive mode (REPL) by typing `ream` and entering the script above. If everything is set up correctly, you should see `Hello world!` printed back to the screen.



**Sidenote**: If you are used to using the built-in `print` function in other languages, you can store the `IO.Println` function into a global variable like this.

```ream
import IO
global print = IO.Println

// Now you can write to the screen like this
print('Hello world!')
```



### Fundamentals

#### Execution

Code in Ream is executed line-by-line and uses newline characters to determine how the code should be parsed. You can override this using the `;` and `\` characters. The semicolon inserts a newline to be read by the parser, whereas the backslash character suppresses the next character (if it's a newline).

The REPL can detect unfinished expressions and statements, and will reply with a continuation prompt `. ` (with the current indentation level) until it is complete. This enables the ability to write functions, classes and other multiline blocks within the REPL (although this can be done using the newline character even though it is harder to read)

Ream is designed to return `null` when something fails or is invalid, e.g. `a.b == null` will return `true` if `a` hasn't been declared and doesn't have a property of `b`. This behavior can be altered by setting `Flags.Strict = true`, but note that this will only effect some of these scenarios, it will not effect retrieving an unassigned variable, but it will effect attempting to call that invalid variable (since it will be `null`). It will also effect whether the `import` statement should throw an error in the case of the file not being found.

Ream can sometimes be confused as to whether the block of code is a statement or expression, it defaults to a statement, as shown here.

```ream
{
	a = 5
}
```

But sometimes this behavior can be unwanted, for example, you are creating a dictionary.

```ream
{
	'foo': 'bar'
}
```

This will assume the block is a statement, but we know its an expression, this will throw an error since `'foo': 'bar'` is an invalid block. This issue can be resolved by using the `::` expression operator. It is included before the expression itself, like so.

```ream
:: {
	'foo': 'bar'
}
```

Note that most times that you use a dictionary, it will be in a place that expects an expression, e.g. `a = {'foo': 'bar'}` or `doSomething({'foo': 'bar'})`



#### Types

Ream uses dynamic typing to store data, which allows for flexibility within the language. There are 9 primitive types, `null`, `Boolean`, `Number`, `String`, `Sequence`, `Dictionary`, `Prototype`, `Pointer` and `Callable`. You can retrieve the type of an object using the `Flags.Type` function. Its usage is as follows.

```ream
import IO // Just for showing the results

IO.Println(Flags.Type(null))                           // null
IO.Println(Flags.Type(true))                           // Boolean
IO.Println(Flags.Type(1234))                           // Number
IO.Println(Flags.Type('Hello world!'))                 // String
IO.Println(Flags.Type(['apple', 'banana', 'orange']))  // Sequence
IO.Println(Flags.Type({'foo': 'bar'}))                 // Dictionary
IO.Println(Flags.Type(<>))                             // Prototype
IO.Println(Flags.Type(&Flags))                         // Pointer
IO.Println(Flags.Type(Flags.Type))                     // Callable
```



##### Null

Null is an empty value. It is used to represent when something does not exist, isn't possible or is invalid. Null, when casted to a Boolean, returns false.



##### Boolean

A Boolean value can be be either true or false, this can be used to control conditions and expressions (Such as if statements and ternary expressions). You can convert any type to a Boolean by using a double not unary expression. E.g. `!!'hi'`  This will return true since strings will evaluate as true when their length is larger than 0.



##### Number

Numeric values store numbers, the limits are 2^1024 to 2^1024-53, based on the C# `double` datatype. The Boolean representation of the number is true, if the number is larger than 0, or false if less/equal to.



##### String

A string is basically text, it can use either single or double quotes to act as delimiters. Strings are immutable, they cannot be modified after creation, instead a new string can be created based on the existing one. The following escape characters can be expressed in strings, allowing access to various special characters: `\a`, `\b`, `\f`, `\n`, `\r`, `\t`, `\v`, `\0`, `\\`, `\'`, `\"`, `\{` and `\}`. Strings support multiple lines, but not in the REPL, this is due to the way it handles tokens. If you put a `$` before the start of a string, like this `$'Hello world'`, it becomes an interpolated string, allowing you to perform expressions within the string.

```ream
import IO

a = 5
b = 10

IO.Println($'Hello! the sum of a and b is {a + b}')
```

This also supports nested strings/expressions and will automatically convert the expression result to a string. Strings, when casted to a Boolean will evaluate as true when their length is larger than 0, else false.



##### Sequence

A sequence is an array of values, it has no fixed size, its elements can be of any type. You can declare one like this: `a = ['apple', 'banana', 'orange']`, an empty one can be simply be declared here: `a = []`. Elements can be added using the `Sequence.Add` function like this:

```ream
a = []
a.Add('apple')
// a now looks like this: ['apple']
a.Add('banana')
// a now looks like this: ['apple', 'banana']
a.Add('orange')
// a now looks like this: ['apple', 'banana', 'orange']
```

Elements can be removed by their value or index, using the `Sequence.Remove` and `Sequence.Delete`, respectively. You can retrieve/set a value from an index like this:

```ream
import IO

a = ['apple', 'banana', 'orange']
IO.Println(a[0]) // Prints apple
a[0] = 'grape'
IO.Println(a[0]) // Prints grape
```

Just note, you cannot assign to an index that does not exist, e.g assigning to `a[3]` when only 3 elements are present (`0`, `1`, `2`). This will result in nothing happening.



##### Dictionary

##### Prototype

##### Pointer

##### Callable



### Elaboration

#### Attributes

Variables and Functions in Ream can have "attributes", a variable's attributes are defined at declaration, but can be re-declared in order to change them. These are `Normal`, `Local`, `Global`, `Dynamic`, `Final` and `Static`. By default, when no attributes are given, the Variable/Function is assumed to be `Normal`. They are supplied similarly to an assignment, as followed. They are also stackable (multiple attributes can be applied to the same variable/function).

```ream
// For variables
local a = 5 // <lowercase_attribute_name> <variable_name> = <expression/constant>

// For functions
<lowercase_attribute_name> function <function_name> [: [optional_argument_names]*] {
	<block content>
}
```

Note that the `Normal` type cannot be explicitly provided at declaration.

##### Local

The local attribute forces the variable/function to be defined in the current scope, the interpreter will make no attempts to search in parent scopes for an existing definition.



##### Global

The global attribute defines the variable/function in the topmost scope, meaning it can be accessed from anywhere using the same interpreter.



##### Dynamic

The dynamic attribute is only functional on a variable, instead of storing the evaluated value, it stores the expression itself, and evaluates it when it is requested, e.g.

```ream
import IO

a = 5
b = 10
dynamic c = a + b
IO.Println(c) // Will print 15

a = 10
IO.Println(c) // Will print 20
```



##### Final

The final attribute is read-only when it's value is not null, explained below.

```ream
import IO

final a = 5
IO.Println(a) // Will print 5
a = 10
IO.Println(a) // Will print 5
```



##### Static

The static attribute signifies to the class (if it is in one), that the variable/function belongs to the static scope of the class, not the instances of it.



### Conventions



### Quirks

TODO:

- Mention indexing an Number returns indexer
