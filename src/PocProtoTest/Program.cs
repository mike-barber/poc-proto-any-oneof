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

        static void TestWrapperOneOf()
        {
            var msg1 = new Message1 { Info = "Message 1" };
            var msg2 = new Message2 { Info = "Message 2", ExtraInfo = "Hi!" };

            var wrap1 = new WrapperOneOf();
            wrap1.Msg1 = msg1;
            Console.WriteLine(wrap1);

            var wrap2 = new WrapperOneOf();
            wrap2.Msg2 = msg2;
            Console.WriteLine(wrap2);
        }


        const string typeUrlPrefix = "flutter.com";
        
        static string GetTypeUrl(IMessage msg)
        {
            return $"{typeUrlPrefix}/{msg.Descriptor.FullName}";
        }

        static void TestWrapperAny()
        {
            var msg1 = new Message1 { Info = "Message 1" };
            var msg2 = new Message2 { Info = "Message 2", ExtraInfo = "Hi!" };
            var msg3 = new Message3 { Info = "Message 3", ExtraInfo = "Hi 3!", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) };

            // manually pack
            var wrap1 = new WrapperAny();
            wrap1.Message = new Any()
            {
                // note: abusing the TypeUrl here -- it's really meant to be a URL
                TypeUrl = GetTypeUrl(msg1),
                Value = msg1.ToByteString()
            };

            
            var wrap2 = new WrapperAny();
            wrap2.Message = new Any()
            {
                TypeUrl = GetTypeUrl(msg2),
                Value = msg2.ToByteString()
            };

            // use Any to pack
            var wrap3 = new WrapperAny()
            {
                Message = Any.Pack(msg3, typeUrlPrefix)
            };

            void Test(WrapperAny wrap)
            {
                // serialize
                var bytes = wrap.ToByteArray();

                // deserialize
                var deser = WrapperAny.Parser.ParseFrom(bytes);

                Console.WriteLine(deser);
            }
            Test(wrap1);
            Test(wrap2);
            Test(wrap3);
        }

        static void TestWrapperCustom()
        {
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

            void ConditionalDecode<T>(WrapperCustom w, Action<T> action)
                where T: IMessage, new()
            {
                var m = new T();
                if (m.Descriptor.FullName == w.TypeId)
                {
                    m.MergeFrom(w.Message);
                    action(m);
                }
            }


            void Test(WrapperCustom wrap)
            {
                // serialize
                var bytes = wrap.ToByteArray();

                // deserialize
                var deser = WrapperCustom.Parser.ParseFrom(bytes);

                Console.WriteLine(deser);

                ConditionalDecode<Message1>(deser, m => Console.WriteLine("Message1!"));
                ConditionalDecode<Message2>(deser, m => Console.WriteLine("Message1!"));
                ConditionalDecode<Message3>(deser, m => Console.WriteLine("Message1!"));
            }
            Test(wrap1);
            Test(wrap2);
            Test(wrap3);
        }
    }
}
