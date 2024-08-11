

How to use Tempo
===


# How to search for an API

Type all or part of an API name into the search box,
either on the main/home page or on the search results page (to do a new search).

The search is case insensitive by default, and looks at:

* Type names
* Member names
* Parameter and return value names and types
* Attributes on types or members
* Other type info, such as the base type or the name of the DLL

# Using Regex or Wildcard Syntax

You don't need to use either, but you can use either Regex or wildcard syntax in a search string.
Choose which on the home page below the search box:

![screenshot of regex or wildcard syntax menu](Assets/help-syntax.jpg)

## Regex info

Some useful Regex tools:

* `.*` finds any number of any character
* `foo$` finds anything that _ends_ in "foo"
* `^foo` finds anything that _begins_ with "foo"

Regex examples:

* `button` finds both Button and AppBarButton, but also ContentDialog.CloseButtonClick
* `^button` finds Button and ButtonBase, but not AppBarButton (because it doesn't begin with "button")
* `button$` finds AppBarButton and Button, but not ButtonBase (because it doesn't end with "button")
* `button.*peer` finds ButtonAutomationPeer 

## Wildcard info

In wildcard syntax "." means any character, and "*" means any number of any character.

Note that search strings aren't substrings ("contains").
So for example `button` finds Button but not ButtonBase or ToggleButton.
To find all three, search for `*button*`.


# Type::Member search syntax

You can search for a member of a type using "::".
For example

`button::click` finds the ButtonBase.Click event.

# Searching with Advanced Query Syntax

You can also use AQS to restrict searches, which are in the form of "Property:Value".

For example, this searches the APIs for "button", but only in a "Controls" namespace,
such as Windows.UI.Xaml.Controls:

```
button Namespace:controls
```

This searches for methods whose name begins with "Try" and return void:

```
^Try IsMethod:True ReturnType:void
```

Note that the search term needs to be before the AQS,
for example `button Namespace:controls` rather than `Namespace:controls button`.
You can though have only AQS, for example `Namespace:controls IsType:True`.

You can combine terms with AND, OR, and NOT, or equivalently "&&", "||", and "!".
You can also wrap in parens.
Properties and values are case-insensitive.

If you put two terms next to each other, AND is assumed.
For example these are equivalent:

```
IsType:True && Namespace:(controls || devices)
IsType:True AND Namespace:(controls OR devices)
```

To see all properties available, look at a type or member and click on
the "All model properties" link.
That will show you an example for that item of all the properties and values.

![screenshot of link to all model properties list](Assets/all-model-properties-link.jpg)

# Launching from the command-line (with parameters)

You can launch Tempo with simply:

```
Tempo
```

(Note that there's a PowerShell bug where the command doesn't return to
the command line until the app is closed.
You can work around this by using `start tempo` syntax.)

You can provide a metadata file on the command line.
This will load the file as the "Custom API Scope".
For example:

```
tempo Component.dll
tempo Component.winmd
tempo Component.nupkg
```

You can also compare two files,
setting one file as the "Baseline" and
the other as the "Custom API Scope"

```
tempo /diff Component_V1.nupkg Component_V2.nupkg
tempo /diff Component_V1.dll Component_V2.dll
```

You can also protocol launch:

```
tempo:Button
tempo:Button?scope=windows
tempo:Button?scope=winappsdk
tempo:?scope=Windows
```
