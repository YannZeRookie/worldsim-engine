Notes
=====

Architecture
------------

![Architecture](architecture.png "Architecture")

TODO
----

- Clean-up of the API (accessors, World factory, etc)
- Document the API interfaces
- More defensive code, better error handling, etc. Don't trust the YAML files
- Still too many casts in World.cs
- Importer: load the `scenario` block
- Debug resources with `null` units
- Units should have a conversion table when possible. Define standard units with implicit conversions
- Bug: interactive mode will fail if `time.end` is not set in file

