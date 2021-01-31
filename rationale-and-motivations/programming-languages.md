# Programming languages

"What language do you write stuff in so as to be maximally future-proof?" is
a question I've thought about a fair bit.

In the 1990s, the answer would unquestionably have been "C". It doesn't get
much better than writing a C program in 1992 and having it compile and run
perfectly, 30 years later, exactly the same as it did when it was first written.

In 2002, the answer could very well have been "Python 2". But now that Python 3
has kind of gone off in the weeds, I've been casting about for a new answer.
Golang seems promising, but 30 years is a long time, so who knows.

The big problem with C, today, would of course be libraries and ecosystem.

C# seems awesome. I don't know how usable dotnet (or mono) on linux are. But C#
is non-annoying and I think I trust it to be around and supported for quite a
while.

Another clue is [Microsoft's own list of supported
languages](https://docs.microsoft.com/en-us/azure/developer/) for Azure
development. They list Python, so, as creaky and weird as Python 3 is, it
may be an acceptable alternative as well. And they don't list Golang, so,
well, go figure.

Conclusion: I'm going to try to stick to C# for as much as I can. Exceptions:
* There are some things that C# will never be able to do, e.g. `fork()`. I
  will use Python 3 for anything that needs those.
* For "build a quick one-off", Python still has the edge. These are things
  that quite frankly don't *need* to still be around in 30 years.
