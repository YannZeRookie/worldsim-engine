WorldSim Simulation Engine
==========================

About
-----

This is a Climatic Simulation Engine that is intended to be used in the WorldSim project developed by
the [Ogee Foundation](https://www.ogeefoundation.org/).

It is designed as pure C# classes with a clean API so that it can be easily integrated as a library in another C#
environment, such as Unity.

The dependencies are minimal. For the most part, it's only
the [Cinchoo ETM Yaml Reader](https://www.codeproject.com/Articles/5272376/Cinchoo-ETL-Yaml-Reader).

The file format is based on YAML, so that files are easy for humans to edit.

Key Principles
--------------

The Simulation Engine is completely agnostic in terms of what it manipulates. As far as it is concerned, it merely
computes float values that you choose to name the way you want and that have arbitrary units that you choose. Some of
these values are "produced", some others are "consumed" - hence their "resource" name. All the Simulation Engine does is
managing stock levels and trying to match demand with supply. When resources are depleted, production becomes impacted
on a pro-rata basis of the most severe shortage (weakest resource rule).

Examples
--------

Examples of YAML simulation files can be found in the [documentation folder](engine/doc/) and in
the [test fixtures folder](tests/fixtures/). Use these to get started.

Using the Command Line
----------------------

By default, the purpose of the command line is to load a YAML file, run the simulation until it reaches its Current
Time, and display the resulting KPIs:

    $ ./engine my_simulation.yaml
    #165: 1965-01-01: Available Coal= 83000.0 T Atmosphere CO2=  8200.0 T

You can show the steps from the Start Time to the Current Time by using the `-v` verbose option:

    $ ./engine -v my_simulation.yaml
    #  0: 1800-01-01: Available Coal=     0.0 T Atmosphere CO2=     0.0 T
    #  1: 1801-01-01: Available Coal=  1000.0 T Atmosphere CO2=     0.0 T
    #  2: 1802-01-01: Available Coal=  1500.0 T Atmosphere CO2=    50.0 T
    #  3: 1803-01-01: Available Coal=  2000.0 T Atmosphere CO2=   100.0 T
    ...
    #163: 1963-01-01: Available Coal= 82000.0 T Atmosphere CO2=  8100.0 T
    #164: 1964-01-01: Available Coal= 82500.0 T Atmosphere CO2=  8150.0 T
    #165: 1965-01-01: Available Coal= 83000.0 T Atmosphere CO2=  8200.0 T

You can increase the verbosity by using `-vv`, which shows details about the stocks in each cell. This is convenient for
debugging your simulation:

    $ ./engine -vv my_simulation.yaml
    #  0: 1800-01-01: Available Coal=     0,0 T Atmosphere CO2=     0,0 T
    Cell[0,0]: coal=       0.0 T co2=       0.0 T  Efficiency= 100%
    Cell[1,0]: coal=       0.0 T co2=       0.0 T  Efficiency= 100%
    
    #  1: 1801-01-01: Available Coal=  1000.0 T Atmosphere CO2=     0.0 T
    Cell[0,0]: coal=    1000.0 T co2=       0.0 T  Efficiency= 100%
    Cell[1,0]: coal=       0.0 T co2=       0.0 T  Efficiency=   0%

    ...

    #164: 1964-01-01: Available Coal= 82500.0 T Atmosphere CO2=  8150.0 T
    Cell[0,0]: coal=   82500.0 T co2=       0.0 T  Efficiency= 100%
    Cell[1,0]: coal=       0.0 T co2=    8150.0 T  Efficiency= 100%
    
    #165: 1965-01-01: Available Coal= 83000.0 T Atmosphere CO2=  8200.0 T
    Cell[0,0]: coal=   83000.0 T co2=       0.0 T  Efficiency= 100%
    Cell[1,0]: coal=       0.0 T co2=    8200.0 T  Efficiency= 100%

You can change the date intervals using the `-f` and the `-t` options with dates in the YYYY-MM-DD format. Examples:

Changing the target date of the simulation:

    $ ./engine -t 1850-1-1 my_simulation.yaml
    # 50: 1850-01-01: Available Coal= 25500.0 T Atmosphere CO2=  2450.0 T

Indicating a starting date - note that this implies showing the steps. Keep in mind that a simulation is always run all
the way from its start when it's loaded anyway.

    $ ./engine -f 1840-1-1 -t 1850-1-1 my_simulation.yaml
    # 40: 1840-01-01: Available Coal= 20500.0 T Atmosphere CO2=  1950.0 T
    # 41: 1841-01-01: Available Coal= 21000.0 T Atmosphere CO2=  2000.0 T
    # 42: 1842-01-01: Available Coal= 21500.0 T Atmosphere CO2=  2050.0 T
    # 43: 1843-01-01: Available Coal= 22000.0 T Atmosphere CO2=  2100.0 T
    # 44: 1844-01-01: Available Coal= 22500.0 T Atmosphere CO2=  2150.0 T
    # 45: 1845-01-01: Available Coal= 23000.0 T Atmosphere CO2=  2200.0 T
    # 46: 1846-01-01: Available Coal= 23500.0 T Atmosphere CO2=  2250.0 T
    # 47: 1847-01-01: Available Coal= 24000.0 T Atmosphere CO2=  2300.0 T
    # 48: 1848-01-01: Available Coal= 24500.0 T Atmosphere CO2=  2350.0 T
    # 49: 1849-01-01: Available Coal= 25000.0 T Atmosphere CO2=  2400.0 T
    # 50: 1850-01-01: Available Coal= 25500.0 T Atmosphere CO2=  2450.0 T

For more advanced time settings, it's easier and just as fast to edit your YAML file directly. See the Time section in
the file.

Using `-h` or `--help` gives details about the options:

    $ ./engine --help
    Usage: engine.exe [OPTIONS]+ fileName
    Run a simulation until the `to` date and show the resulting KPIs.
    If no `to` date is specified, the `currentTime` from the file is used.
    If a `from` date is specified, KPIs will be shown for each step.
    
    Options:
    -f, --from=VALUE           start date (YYYY-MM-DD). Implies -v
    -t, --to=VALUE             end date (YYYY-MM-DD)
    -v                         increase details verbosity:
                                   -v show each step
                                   -vv show each step and cells
    -h, --help                 show this message and exit

Building the project
--------------------

The project is developed using [JetBrains Rider](https://www.jetbrains.com/rider/) but you should be able to load and
compile it in Visual Studio as Rider is supposed to be fully compatible (I have not tried it yet, though).

Release Notes
-------------

### 2021-01-02 - Version 0.1:

First working prototype with a running engine and a usable command line to play with. There are still a lot of missing
features, the code is not defensive and the API is not documented. Documentation is yet to be written - see
the [documentation folder](engine/doc/).
