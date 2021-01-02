WorldSim File Format
====================

Identifiers Format
------------------
Identifier follow the [Camel case convention](https://en.wikipedia.org/wiki/Camel_case): WorldSim

```
modDate: "bla bla bla"
```

Versioning
----------

WorldSim uses [semantic versioning](https://semver.org/). 
The version indicated in a WorldSim file indicates for
which version of the Simulation Engine it was designed for.
Higher versions of the Simulation Engine are supposed to know
if they can read the file or not.

IDs
---
Some objects or elements can be identified in a unique manner in a
dictionaries. Their key must follow the `[a-z][a-zA-Z0-9_-]*` format.  