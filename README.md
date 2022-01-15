# Ream
### Overview

The programming language that uses common sense to prevent you from typing more than you need, simple yet powerful.

Check how to contribute and check change log [here](Development.md)

### Examples
Hello world sample
```ream
Main.Write('Hello World!')
```



### Syntax

**Built in**

Write welcome to output

```ream
Main.Write('Welcome')
```

Wait for 5 seconds before continuing

```ream
Main.Sleep(5)
```

This will write the specified token to output for debug purposes

```ream
write 'I am a string!'
```

**Types**

```ream
myStr = 'Hello World!'
myInt = 5
myBool = true
myNull = null
```

**Variables**
This will detect the type, create if it doesn't exist, and set the value
```ream
x = 5
```

Get a variable's content

```ream
x
```



**Functions**
This will create a function with the specified name and parameters

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

**Comparisons**

Check if x is smaller than 10

```ream
x < 10
```

Check if x is larger than 10

```ream
x > 10
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

**Operators**

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

**Scope**
To force something to the top of a scope, use `global`

```ream
global x = 10
global function x
{
	// ...
}
```

**Statements**
If statement
```ream
if true
{
	// Code here...
}
```

**Loops**

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

