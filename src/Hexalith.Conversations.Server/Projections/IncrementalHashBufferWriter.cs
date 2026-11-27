// <copyright file="IncrementalHashBufferWriter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Buffers;
using System.Security.Cryptography;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Bounded pooled <see cref="IBufferWriter{T}"/> that streams written UTF-8 bytes directly into an
/// <see cref="IncrementalHash"/> without materializing large intermediate byte arrays.
/// </summary>
internal sealed class IncrementalHashBufferWriter : IBufferWriter<byte>, IDisposable
{
    private const int DefaultBufferSize = 4096;
    private const int FlushThreshold = 2048;
    private readonly IncrementalHash _hash;
    private byte[] _buffer;
    private int _written;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementalHashBufferWriter"/> class.
    /// </summary>
    /// <param name="hash">The incremental hash receiving streamed bytes.</param>
    internal IncrementalHashBufferWriter(IncrementalHash hash)
    {
        _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
    }

    /// <inheritdoc/>
    public void Advance(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > _buffer.Length - _written)
        {
            throw new InvalidOperationException("Cannot advance past the rented buffer length.");
        }

        _written += count;
        if (_written >= FlushThreshold)
        {
            Flush();
        }
    }

    /// <inheritdoc/>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <inheritdoc/>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    /// <summary>Appends any buffered bytes to the underlying hash and resets the buffer position.</summary>
    internal void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_written > 0)
        {
            _hash.AppendData(_buffer.AsSpan(0, _written));
            _written = 0;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        byte[] buffer = _buffer;
        if (buffer.Length > 0)
        {
            _buffer = [];
            _written = 0;
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private void EnsureCapacity(int sizeHint)
    {
        int required = Math.Max(sizeHint, 1);
        if (_buffer.Length - _written >= required)
        {
            return;
        }

        Flush();
        if (_buffer.Length >= required)
        {
            return;
        }

        byte[] previous = _buffer;
        _buffer = ArrayPool<byte>.Shared.Rent(required);
        ArrayPool<byte>.Shared.Return(previous, clearArray: true);
    }
}
