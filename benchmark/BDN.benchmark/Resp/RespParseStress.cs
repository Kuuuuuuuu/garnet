﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Embedded.perftest;
using Garnet.server;

namespace BDN.benchmark.Resp
{
    [MemoryDiagnoser]
    public unsafe class RespParseStress
    {
        EmbeddedRespServer server;
        RespServerSession session;

        const int batchSize = 128;

        static ReadOnlySpan<byte> INLINE_PING => "PING\r\n"u8;
        byte[] pingRequestBuffer;
        byte* pingRequestBufferPointer;

        static ReadOnlySpan<byte> SET => "*3\r\n$3\r\nSET\r\n$1\r\na\r\n$1\r\na\r\n"u8;
        byte[] setRequestBuffer;
        byte* setRequestBufferPointer;

        static ReadOnlySpan<byte> GET => "*2\r\n$3\r\nGET\r\n$1\r\nb\r\n"u8;
        byte[] getRequestBuffer;
        byte* getRequestBufferPointer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var opt = new GarnetServerOptions
            {
                QuietMode = true
            };
            server = new EmbeddedRespServer(opt);
            session = server.GetRespSession();

            pingRequestBuffer = GC.AllocateArray<byte>(INLINE_PING.Length * batchSize, pinned: true);
            pingRequestBufferPointer = (byte*)Unsafe.AsPointer(ref pingRequestBuffer[0]);
            for (int i = 0; i < batchSize; i++)
                INLINE_PING.CopyTo(new Span<byte>(pingRequestBuffer).Slice(i * INLINE_PING.Length));

            setRequestBuffer = GC.AllocateArray<byte>(SET.Length * batchSize, pinned: true);
            setRequestBufferPointer = (byte*)Unsafe.AsPointer(ref setRequestBuffer[0]);
            for (int i = 0; i < batchSize; i++)
                SET.CopyTo(new Span<byte>(setRequestBuffer).Slice(i * SET.Length));

            getRequestBuffer = GC.AllocateArray<byte>(GET.Length * batchSize, pinned: true);
            getRequestBufferPointer = (byte*)Unsafe.AsPointer(ref getRequestBuffer[0]);
            for (int i = 0; i < batchSize; i++)
                GET.CopyTo(new Span<byte>(getRequestBuffer).Slice(i * GET.Length));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            session.Dispose();
            server.Dispose();
        }

        [Benchmark]
        public void InlinePing()
        {
            _ = session.TryConsumeMessages(pingRequestBufferPointer, pingRequestBuffer.Length);
        }

        [Benchmark]
        public void Set()
        {
            _ = session.TryConsumeMessages(setRequestBufferPointer, setRequestBuffer.Length);
        }

        [Benchmark]
        public void Get()
        {
            _ = session.TryConsumeMessages(getRequestBufferPointer, getRequestBuffer.Length);
        }
    }
}