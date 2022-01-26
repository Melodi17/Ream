# Ream
## Overview

The programming language that uses common sense to prevent you from typing more than you need, simple yet powerful.

Check how to contribute and check change log [here](Development.md)

## Examples
Hello world sample
```ream
write('Hello World!')
```



## Syntax

**Warning:** Syntax is still subject to change

### Built in

Write welcome to output

```ream
write('Welcome')
```

Read text from input

```ream
x = read('What is your name?')
```

Wait for 5 seconds before continuing

```ream
sleep(5)
```

### Types

```ream
myStr = 'Hello World!'
myInt = 5
myBool = true
myNull = null
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
    write(a + b)
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
	write('Hello ' + name)
}

x('Joe')
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

### Scope

To force something to the top of a scope, use `global`

```ream
global x = 10
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

