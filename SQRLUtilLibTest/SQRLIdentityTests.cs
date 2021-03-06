﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using SQRLUtilsLib;
using System.Linq;

namespace SQRLUtilLibTest
{
    public class SQRLIdentityTests
    {
		[Fact]
		public void FromToByteArrayTest()
		{
			List<Tuple<byte[], bool>> idVectors = new List<Tuple<byte[], bool>>()
			{
				// "Ordinary" identity containing block 1 + 2
				Tuple.Create(Sodium.Utilities.HexToBinary("7371726C646174617D0001002D003DF8C1D0D35425CBABE1ECEA13100FCDA8CE1EE9CC6C88A0F512D5F60991000000F30104050F00B10BC086752714EF4AC4330015268AF7716F8AA231C7F912D217189D37BEC4F82CBFF30D7ADC9ECF361E9236BC5E66FCBB6B75E6B7B23D5543F3DC78B50071C77A6CED382249904789E1F6341A91E9DC490002002FBE0BA95BD8FF50053728A565DD3EEF0995000000E64C5A1EDDB214D6B2886AC3FDA1E831C3DDFDD6405035F69E1AFAD21464C75E98CCD59C5A8BE47E39F2A39F2D10D3BA"), false),
				// Rekeyed 4 times, thus containing a large block 3
				Tuple.Create(Sodium.Utilities.HexToBinary("7371726C646174617D0001002D002943D5681A77E7EE06F6878C830D41F1B39BBBF63A9FA58B2022D4DE0990000000F30104050F00066DBAF9D304A4011E980AE2F43C489AD1F8AAC7EB5397F875C06B231D56AFEC1A398E1A5703FB622DEB645A2E77FF456976A312F07F555C5D3407DB23DD47A777CAB5683564FCB677BA63D93E3AF2F249000200415464EDD9EF881606D85D01EBDF0D21098F00000096FA59AD8B58DE81A4ED00C2516CE80BD65CC1389C0A7A89F85F775DBB455E0AF1CF5757F11B17924DC1EB6863D82C20960003000400B19A77C75B9945BEB48BFCC6494244BC5C025EB9F4501017FF04E41C02B7B65ECA95D164C4465AAC3140292598592E56D9A88FCF787B5189BDFD81E238641CE46B0C97E47D5EBDC4DD16D5748874C8A5BFA82515F50F1577E15A157CDD059695A61BF0538217680019E5D9AF34A1F70054A057BFFC6F505E1859FFDCA2EB418F0F1B3903C78B219BDB5D28620BB1BFEB"), false),
				// .sqrc-File (containing only block 2)
				Tuple.Create(Sodium.Utilities.HexToBinary("7371726C64617461490002002DD4DEA2238AFF0A553C76AADE9488A309960000006CC320A5DF90DEF5E3F8DCF460B6A5490E7C4CABB93926B0B79D850582C88DE1E698A4F03CC93926CA0015D673DB1686"), false),
				// Custom block (contains a block of imaginary type 55, which should be retained)
				Tuple.Create(Sodium.Utilities.HexToBinary("7371726C646174617D0001002D005B6648EBCC4E296A4CBF479E43C67DDBBBCCEBF73E1D5939D18758D3091F000000F30104050F002C6258339401919F4BA3C1B904174BCA18ED3DC25560637D684A2A7C19761709AED12E480CCED7B0D8B21FC13B773578ABA9EC6838B1ED4D110787EFEB9740EFF40536CC5F598A5714EBBF6E2946F0934900020002D86D2A4CEDC1C91951E5410A5DE3CC09200000000752657E822696708F7DCC96D0AD911C0184BD8A06DE16C7509CED8762B53EBAEEA49CB089449204317437BECC5956DB1500370074657374696E67206974206F7574212129"), false),
				// Base64url-encoded identity (indicated by uppercase "SQRLDATA" header)
				Tuple.Create(Sodium.Utilities.HexToBinary("5351524C44415441665141424143304150666A42304E4E554A63757234657A71457841507A616A4F48756E4D6249696739524C5639676D5241414141387745454251384173517641686E556E464F394B78444D414653614B3933467669714978785F6B53306863596E54652D78506773765F4D4E657479657A7A59656B6A6138586D623875327431357265795056564438397834745142787833707337546769535A4248696548324E4271523664784A414149414C37344C715676595F3141464E79696C5A64302D37776D5641414141356B786148743279464E6179694772445F61486F4D6350645F645A41554458326E6872363068526B783136597A4E5763576F766B666A6E796F353874454E4F36"), true)
			};

			foreach (var idVector in idVectors)
			{
				SQRLIdentity id = SQRLIdentity.FromByteArray(idVector.Item1);
				byte[] result = id.ToByteArray();

				if (!idVector.Item2) // No base 64, straight comparison possible
				{
					Assert.Equal(idVector.Item1, result);
				}
				else                 // Take base64url encoding of source vector into account!
				{
					Assert.Equal(Encoding.UTF8.GetString(idVector.Item1.Skip(8).ToArray()),
						Sodium.Utilities.BinaryToBase64(result.Skip(8).ToArray(), Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
				}
			}
		}
	}
}
