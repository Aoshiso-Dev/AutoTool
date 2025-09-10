using System;
using System.Linq;

namespace AutoTool.Core.Commands
{
    public static class CommandBlockExtensions
    {
        public static CommandBlock? FindBlock(this IHasBlocks owner, string name)
            => owner.Blocks.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.Ordinal));
    }
}
