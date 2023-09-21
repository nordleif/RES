using Mono.Options;
using RES;
using System;

var commands = new CommandSet("res")
{
    "usage: res [COMMAND]",
    "COMMANDS:",
    new ParseCommand(),
};

return commands.Run(args);


