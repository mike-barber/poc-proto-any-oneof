using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Net.NetworkInformation;
using Test.Schema;

namespace PocProtoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TestWrapperOneOf();
            TestWrapperAny();
            TestWrapperCustom();
        }

        static void Title(string title)
        {
            Console.WriteLine();
            Console.WriteLine("************************");
            Console.WriteLine($"* {title}");
            Console.WriteLine("************************");
        }

        /// <summary>
        /// Using a wrapper with `oneof` to contain several types
        /// </summary>
        static void TestWrapperOneOf()
        {
            Title("WrapperOneOf and WrapperOneOfEarlier");

            var msg1 = new Message1 { Info = "Message 1" };
            var msg2 = new Message2 { Info = "Message 2", ExtraInfo = "Hi!" };
            var msg3 = new Message3 { Info = "Message 3", ExtraInfo = "Hi 3!", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) };

            var wrap1 = new WrapperOneOf { Msg1 = msg1 };
            Console.WriteLine(wrap1);

            var wrap2 = new WrapperOneOf { Msg2 = msg2 };
            Console.WriteLine(wrap2);

            var wrap3 = new WrapperOneOf() { Msg3 = msg3 };

            /**
             * Test decoding message using same schema, and use switch to pick
             * the correct part of the union
             */
            static void TestSame(byte[] payload)
            {
                var w = WrapperOneOf.Parser.ParseFrom(payload);
                switch (w.MessageCase)
                {
                    case WrapperOneOf.MessageOneofCase.Msg1:
                        Console.WriteLine($"Message1 {w.Msg1}");
                        break;
                    case WrapperOneOf.MessageOneofCase.Msg2:
                        Console.WriteLine($"Message2 {w.Msg2}");
                        break;
                    case WrapperOneOf.MessageOneofCase.Msg3:
                        Console.WriteLine($"Message3 {w.Msg3}");
                        break;
                    default:
                        Console.WriteLine($"Unknown case: {w.MessageCase}; message is {w}");
                        break;
                }
            }
            TestSame(wrap1.ToByteArray());
            TestSame(wrap2.ToByteArray());
            TestSame(wrap3.ToByteArray());

            /**
             * Test decoding message using an *earlier* schema, and check 
             * what happens in the case of Message3.
             */
            static void TestEarlier(byte[] payload)
            {
                var w = WrapperOneOfEarlier.Parser.ParseFrom(payload);
                switch (w.MessageCase)
                {
                    case WrapperOneOfEarlier.MessageOneofCase.Msg1:
                        Console.WriteLine($"Message1 {w.Msg1}");
                        break;
                    case WrapperOneOfEarlier.MessageOneofCase.Msg2:
                        Console.WriteLine($"Message2 {w.Msg2}");
                        break;
                    // note: Message3 is not included in the earlier schema, so 
                    //       we're expecting it do hit the default case here.
                    default:
                        Console.WriteLine($"Unknown case: {w.MessageCase}; message is {w}");
                        break;
                }
            }
            TestEarlier(wrap1.ToByteArray());
            TestEarlier(wrap2.ToByteArray());
            TestEarlier(wrap3.ToByteArray());
        }


        const string typeUrlPrefix = "company.com";
        static string GetTypeUrl(IMessage msg)
        {
            return $"{typeUrlPrefix}/{msg.Descriptor.FullName}";
        }
        
        /// <summary>
        /// Using a wrapper with `any` to contain several types
        /// </summary>
        static void TestWrapperAny()
        {
            Title("WrapperAny");

            var msg1 = new Message1 { Info = "Message 1" };
            var msg2 = new Message2 { Info = "Message 2", ExtraInfo = "Hi!" };
            var msg3 = new Message3 { Info = "Message 3", ExtraInfo = "Hi 3!", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) };

            // manually pack
            var wrap1 = new WrapperAny();
            wrap1.Message = new Any()
            {
                // we're abusing this a bit here; the URL is meant to kind of work, not just be a 
                // basic type discriminator.
                TypeUrl = GetTypeUrl(msg1),
                Value = msg1.ToByteString()
            };

            // manually pack 
            var wrap2 = new WrapperAny();
            wrap2.Message = new Any()
            {
                TypeUrl = GetTypeUrl(msg2),
                Value = msg2.ToByteString()
            };

            // use Any class to pack -- does the same thing
            var wrap3 = new WrapperAny()
            {
                Message = Any.Pack(msg3, typeUrlPrefix)
            };

            static void Test(byte[] payload)
            {
                // deserialize
                var w = WrapperAny.Parser.ParseFrom(payload);

                Console.WriteLine($"Wrapper is: {w}");

                if (w.Message.TryUnpack(out Message1 m1))
                    Console.WriteLine("Deserialized Message1: " + m1);

                if (w.Message.TryUnpack(out Message2 m2))
                    Console.WriteLine("Deserialized Message2: " + m2);

                if (w.Message.TryUnpack(out Message3 m3))
                    Console.WriteLine("Deserialized Message3: " + m3);
            }

            Test(wrap1.ToByteArray());
            Test(wrap2.ToByteArray());
            Test(wrap3.ToByteArray());
        }

        /// <summary>
        /// Using a wrapper with a custom equivalent of `any` to contain several types; does pretty much the same thing
        /// but without the implicit restrictions around the formatting of the type URI.
        /// </summary>
        static void TestWrapperCustom()
        {
            Title("WrapperCustom");

            var msg1 = new Message1 { Info = "Message 1" };
            var msg2 = new Message2 { Info = "Message 2", ExtraInfo = "Hi!" };
            var msg3 = new Message3 { Info = "Message 3", ExtraInfo = "Hi 3!", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) };

            var wrap1 = new WrapperCustom()
            {
                TypeId = Message1.Descriptor.FullName,
                Message = msg1.ToByteString()
            };

            var wrap2 = new WrapperCustom()
            {
                TypeId = Message2.Descriptor.FullName,
                Message = msg2.ToByteString()
            };

            var wrap3 = new WrapperCustom()
            {
                TypeId = Message3.Descriptor.FullName,
                Message = msg3.ToByteString()
            };

            // essentially the same idea as previous Any.TryUnpack
            void ConditionalDecode<T>(WrapperCustom w, Action<T> action)
                where T : IMessage, new()
            {
                var m = new T();
                if (m.Descriptor.FullName == w.TypeId)
                {
                    m.MergeFrom(w.Message);
                    action(m);
                }
            }

            void Test(byte[] payload)
            {
                // deserialize
                var w = WrapperCustom.Parser.ParseFrom(payload);

                Console.WriteLine($"Wrapper is: {w}");

                ConditionalDecode<Message1>(w, m => Console.WriteLine($"Message1: {m}"));
                ConditionalDecode<Message2>(w, m => Console.WriteLine($"Message2: {m}"));
                ConditionalDecode<Message3>(w, m => Console.WriteLine($"Message3: {m}"));
            }
            Test(wrap1.ToByteArray());
            Test(wrap2.ToByteArray());
            Test(wrap3.ToByteArray());
        }
    }
}
