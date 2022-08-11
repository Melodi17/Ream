# Ream
## Overview

The programming language that uses common sense to prevent you from typing more than you need, simple yet powerful.

Check how to contribute and check change log [here](Development.md)

You can join the official discord [here](https://discord.gg/ExAfTcf8Nt)

## Examples
Hello world sample
```ream
Main.WriteLine('Hello World!')
```

## Syntax

**Warning:** Syntax is still subject to change

### Built in

Write welcome to output

```ream
Main.WriteLine('Welcome')
```

Read text from input

```ream
x = Main.Read('What is your name?')
```

Wait for 5 seconds before continuing

```ream
Main.Sleep(5)
```

Convert a string to a number

```ream
myStr = '5'
myInt = Cast.Number(myStr)
```

Convert a number to a string

```ream
myInt = 5
myStr = Cast.String(myInt)
```

Get the length of a string

```ream
myStr.Length
```

### Types

```ream
myStr = 'Hello World!'
myInt = 5
myBool = true
myNull = null
myArr = ['Apple', 'Bannana', 5]
```

### Variables
This will detect the type, create if it doesn't exist, and set the value
```ream
x = 5
```

Get a variable's content

```ream
x
```

### Functions
Create a function with the specified name and parameters

```ream
function add : a b
{
    Main.Write(a + b)
}
```
Call a function with parameters

```ream
add(1, 2)
```

Returning values from function

```ream
function compare : a b
{
	return a == b
}

x = compare('Hello', 'World')
```

You can also use a anonymous function (a lambda) these can also support return values

```ream
x = lambda name
{
	Main.WriteLine('Hello ' + name)
}

x('Joe')
```

 ### Classes

Create a class

```ream
class Lunch
{
	// Code here...
}
```

Create a constructor in a class (can have parameters)

```ream
initializer
{
	Main.WriteLine('You created a instance of Lunch!')
}
```

Create a function in a class (can have parameters)

```ream
myFunction
{
	Main.WriteLine('You called myFunction in Lunch!')
}
```

Create static constructor of a class

```ream
static initializer
{
	Main.WriteLine('You created a instance of Lunch!')
}
```

Set a variable within class

```ream
this.done = true
```



### Comparisons

Check if x is smaller than 10

```ream
x < 10
```

Check if x is smaller or equal to 10

```ream
x <= 10
```

Check if x is larger than 10

```ream
x > 10
```

Check if x is larger or equal to 10

```ream
x >= 10
```

Check if either are true

```ream
x | y
```

Check if both are true

```ream
x & y
```

Check if x and y have the same value

```ream
x == y
```

Check if x and y do not have the same value

```ream
x != y
```

### Operators

Add numbers or string together

```ream
x + y
```

Subtract number from another

```ream
x - y
```

Multiple number or string with a number

```ream
x * y
```

Divide number by another

```ream
x / y
```

Invert a boolean

```ream
!x
```

Negate a number

```ream
-x
```

### Typing

To force an variable/function to the top of a scope, use `global`

```ream
function declareX
{
	global x = 10
}

write(x) // Returns null since it does not exist
delcareX()
write(x) // Returns 10
```

To make variable/function only available in current scope, use `local`

```ream
local x = 10
```

To make a variable read-only once it's value is not null, use `final`

```ream
final x = 10

write(x) // Returns 10
x = 5
write(x) // Returns 10
```

To make a variable evaluated every time it is get, use `dynamic`

```ream
a = 5
b = 2
dynamic x = a + b

write(x) // Returns 7
a = 3
write(x) // Returns 5
```

### Statements

If statement
```ream
if true
{
	// Code here...
}
```

Else statement

```ream
if true
...
else
{
	// Code here...
}
```

### Loops

While loop

```ream
while true
{
	Main.Write('Loading...')
}
```

Repeat code 10 times (without storage variable)

```ream
for 10
{
	Main.Write('Ten times')
}
```

Repeat code 10 times (with storage variable)

```ream
for i : 10
{
	Main.Write(i)
}
```

### Imports

Importing is a very useful part of the language, it allows you to load libraries from external sources, these are dll files, you can install them by downloading the .dll file and placing it in the project directory or install it globally by placing it in `%appdata%\Ream\Libraries`

Import it with the following, don't add the .dll on the end as it is automatically added.

```ream
import <mylib>
```



### Prototypes

A prototype is like a cross between a dictionary and a class. They can have unlimited variables within them but can only be assigned as keywords like so:

```ream
x = <>
x.myvar = "some text"
print(x.myvar)
```

 
