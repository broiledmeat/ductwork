#nullable enable
using System;
using System.Collections.Generic;
using Force.Crc32;

namespace ductwork.Artifacts;

public abstract class Artifact : IArtifact
{
    public string Id { get; protected init; } = string.Empty;
    public uint Checksum { get; protected init; }

    protected static uint CreateChecksum(IEnumerable<object> objs)
    {
        uint checksum = 0;
        
        foreach (var obj in objs)
        {
            checksum = Crc32Algorithm.Append(checksum, obj switch
            {
                byte[] objBytes => objBytes, 
                int objInt => BitConverter.GetBytes(objInt),
                long objLong => BitConverter.GetBytes(objLong),
                _ => Array.Empty<byte>()
            });
        }

        return checksum;
    }

    public override string ToString()
    {
        return $"{GetType().Name}({Id}, {Checksum:x8})";
    }
}