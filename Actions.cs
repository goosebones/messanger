/*
 * Actions that the Messenger program con perform
 *
 * @author Gunther Kroth   gdk6217@rit.edu
 * @file Actions.cs
 */

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Messenger {

/*
 * a class to handle the keyGen operation
 */
public class KeyGen {
	
	/*
	 * generate a public and private key pair
	 * stores each key locally
	 *
	 * @param keySize: number of bits in the keys
	 */
	public void start(int keySize) {
		// generate p
		var primep = new Prime();
		var p = primep.generate(keySize / 2);
		var pBitLength = p.GetByteCount() * 8;

		// generate q
		var primeq = new Prime();
		var q = primeq.generate((keySize - pBitLength));

		var N = p * q;
		var r = (p - 1) * (q - 1);
		BigInteger E = 23227;
		var D = modInverse(E, r);

		// generate the public key in eeeeEE..EEnnnnNN..NN format
		var eArray = sizedByteArray(E);
		var nArray = sizedByteArray(N);
		var publicKeyArray = new byte[eArray.Length + nArray.Length];
		System.Buffer.BlockCopy(eArray, 0, publicKeyArray, 0, eArray.Length);
		System.Buffer.BlockCopy(nArray, 0, publicKeyArray, eArray.Length, nArray.Length);

		// store public key in filesystem
		var publicKeyString = Convert.ToBase64String(publicKeyArray);
		var publicKey = new PublicKey(publicKeyString);
		var publicJson = JsonConvert.SerializeObject(publicKey, Formatting.Indented);
		// if a Public Key already exists, it is overwritten 
		File.WriteAllText("public.key", publicJson);

		// generate private key in ddddDD..DDnnnnNN..NN format
		var dArray = sizedByteArray(D);
		var privateKeyArray = new byte[dArray.Length + nArray.Length];
		System.Buffer.BlockCopy(dArray, 0, privateKeyArray, 0, dArray.Length);
		System.Buffer.BlockCopy(nArray, 0, privateKeyArray, dArray.Length, nArray.Length);

		// store private key in filesystem
		var privateKeyString = Convert.ToBase64String(privateKeyArray);
		var privateKey = new PrivateKey(privateKeyString);
		var privateJson = JsonConvert.SerializeObject(privateKey, Formatting.Indented);
		// if a Private Key already exists, it is overwritten
		File.WriteAllText("private.key", privateJson);
	}

	/*
	 * create a byte array that is used in a key
	 * the first 4 bytes will represent the size of the E bytes array 
	 * the next e bytes will be the byte array representation of E
	 * eeeeEE.EE format
	 *
	 * @param E: the number to create a byte array for
	 * @return a byte[] of correct length and format
	 */
	private byte[] sizedByteArray(BigInteger E) {
		// e into byte array
		var eByteArray = E.ToByteArray();
		// size of e into a byte array 
		var eSizeArray = BitConverter.GetBytes(eByteArray.Length);
		// check for endian
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(eSizeArray);
		}
		// combine the e size array and the e byte array 
		var eCompleteArray = new byte[eSizeArray.Length + eByteArray.Length];
		System.Buffer.BlockCopy(eSizeArray, 0, eCompleteArray, 0, eSizeArray.Length);
		System.Buffer.BlockCopy(eByteArray, 0, eCompleteArray, eSizeArray.Length, eByteArray.Length);
		return eCompleteArray;
	}

	/**
	 * compute the mod inverse of two numbers
	 */
	private BigInteger modInverse(BigInteger a, BigInteger n) {
		BigInteger i = n, v = 0, d = 1;
		while (a > 0) {
			BigInteger t = i / a, x = a;
			a = i % x;
			i = x;
			x = d;
			d = v - t * x;
			v = x;
		}
		v %= n;
		if (v < 0) v = (v + n) % n;
		return v;
	}

}

/*
 * a class to handle the sendKey operation
 */
public class SendKey {
	
	/**
	 * send a key to the server
	 * the server will register this email with the local public key
	 * local private key is updated to include this email
	 *
	 * @param email: user email to send to server
	 */
	public async Task start(string email) {
		// check if the user has public and private keys
		if (!File.Exists("public.key")) {
			Console.WriteLine("No public key found. Unable to register " + email + " to server without a public key.");
			return;
		}
		if (!File.Exists("private.key")) {
			Console.WriteLine("No private key found. Generate one before assigning " + email + " to it.");
			return;
		}
		
		// add the email to the local private key
		var privateKeyJson = File.ReadAllText("private.key");
		var privateKey = JsonConvert.DeserializeObject<PrivateKey>(privateKeyJson);
		
		// local file has gets the email appended to registered addresses
		privateKey.addEmail(email);
		privateKeyJson = JsonConvert.SerializeObject(privateKey, Formatting.Indented);
		
		// write the new Private Key to local files
		File.WriteAllText("private.key", privateKeyJson);
		
		// make a public key Json object
		var publicKeyJson = File.ReadAllText("public.key");
		var publicKey = JsonConvert.DeserializeObject<PublicKey>(publicKeyJson);
		publicKey.addEmail(email);
		publicKeyJson = JsonConvert.SerializeObject(publicKey);
		
		// send public key to server 
		var client = new HttpClient();
		try {
			var request = "http://kayrun.cs.rit.edu:5000/Key/" + email;
			var content = new StringContent(publicKeyJson, System.Text.Encoding.UTF8, "application/json");
			var response = await client.PutAsync(request, content);
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException e) {
			Console.WriteLine("Error: " + e.Message);
			return;
		}
		
		// if we made it this far, the key was sent successfully
		Console.WriteLine("Key saved");
	}
	
}

/*
 * a class to handle the getKey operation
 */
public class GetKey {
	
	/**
	 * get a key from the server
	 * the public key associated with the supplied email is retrieved
	 * this public key will be stored locally as 'email.key'
	 *
	 * @param email: user email address to retrieve public key for
	 */
	public async Task start(string email) {
		// get the public key from the server
		var client = new HttpClient();
		try {
			var request = "http://kayrun.cs.rit.edu:5000/Key/" + email;
			var response = await client.GetAsync(request);
			response.EnsureSuccessStatusCode();
			var keyJson = await response.Content.ReadAsStringAsync();
			// check if the server returned a json object
			if (keyJson.Equals("")) {
				Console.WriteLine(email + " public key not found.");
				return;
			}
			// save key to filesystem as 'email.key'
			File.WriteAllText(email + ".key", keyJson);

		}
		catch (HttpRequestException e) {
			Console.WriteLine("Error: " + e.Message);
			return;
		}
	}

}

/*
 * a class to handle the getMsg operation
 */
public class GetMsg {
	
	/**
	 * get a message from the server
	 * only messages for emails registered with the private key can be decoded
	 *
	 * @param email: user email to get messages for
	 */
	public async Task start(string email) {
		// check if the user has a private key
		if (!File.Exists("private.key")) {
			Console.WriteLine("No private key found. Generate one before decrypting messages.");
		}
		// get our private key
		var privateKeyJson = File.ReadAllText("private.key");
		var privateKey = JsonConvert.DeserializeObject<PrivateKey>(privateKeyJson);
		// check if we have the key for this email
		if (!privateKey.email.Contains(email)) {
			Console.WriteLine("Private key does not exist for " + email + ". Unable to decrypt message.");
			return;
		}
		
		// get the message from the server
		var client = new HttpClient();
		var m = new Message("");
		try {
			var request = "http://kayrun.cs.rit.edu:5000/Message/" + email;
			var response = await client.GetAsync(request);
			response.EnsureSuccessStatusCode();
			var messageJson = await response.Content.ReadAsStringAsync();
			m = JsonConvert.DeserializeObject<Message>(messageJson);
			if (m.content == null) {
				Console.WriteLine("No messages were available for " + email);
				return;
			}
		}
		catch (HttpRequestException ex) {
			Console.WriteLine("Error: " + ex.Message);
		}
		
		// convert message from Base64 to Byte[]
		var messageByteArray = Convert.FromBase64String(m.content);
		// convert our private key from Base64 to Byte[]
		var keyByteArray = Convert.FromBase64String(privateKey.key);
		
		// decrypt the message
		var P = decrypt(messageByteArray, keyByteArray);
		Console.WriteLine(P);
	}

	/*
	 * decrypt an encoded, encrypted message
	 *
	 * @param CArray: byte[] representing the encrypted message
	 * @param key: byte[] representing the private key
	 */
	private string decrypt(byte[] CArray, byte[] key) {
		// begin by unpacking the key 
		
		// read the first 4 bytes
		var dSizeArray = new byte[4];
		Buffer.BlockCopy(key, 0, dSizeArray, 0, 4);
		// check the endian
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(dSizeArray);
		}
		// convert these 4 bytes to d
		var d = BitConverter.ToInt32(dSizeArray, 0);

		// skip the first 4 bytes and read d bytes into D
		var DArray = new byte[d];
		Buffer.BlockCopy(key, 4, DArray, 0, d);
		// convert D into a BigInteger
		var D = new BigInteger(DArray);

		// skip the first 4 + d bytes and read 4 bytes into n
		var nSizeArray = new byte[4];
		Buffer.BlockCopy(key, d + 4, nSizeArray, 0, 4);
		// check the endian
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(nSizeArray);
		}
		// convert these 4 bytes to n
		var n = BitConverter.ToInt32(nSizeArray, 0);

		// skip the first 4 + d + 4 bytes and read n bytes into N
		var NArray = new byte[n];
		Buffer.BlockCopy(key, 4 + d + 4, NArray, 0, n);
		// convert N into a BigInteger
		var N = new BigInteger(NArray);

		// compute the plaintext message
		var C = new BigInteger(CArray);
		var plainTextBigInteger = BigInteger.ModPow(C, D, N);
		var plainTextByteArray = plainTextBigInteger.ToByteArray();
		var ascii = new ASCIIEncoding();
		return ascii.GetString(plainTextByteArray);
	}

}

/*
 * class to handle the sendMsg operation
 */
public class SendMsg {
	
	/*
	 * send a message to another user
	 * messages can only be send to users for which the public key is known
	 *
	 * @param email: user to send message to
	 * @param plaintext: string message to send to user
	 */
	public async Task start(string email, string plaintext) {
		// first check if we have the public key for this user 
		if (!File.Exists(email + ".key")) {
			Console.WriteLine("Key does not exist for " + email);
			return;
		}
		
		// get the public key for this user
		var publicKeyJson = File.ReadAllText(email + ".key");
		var publicKey = JsonConvert.DeserializeObject<PublicKey>(publicKeyJson);
		
		// encrypt the message
		var messageByteArray = Encoding.ASCII.GetBytes(plaintext);
		var keyByteArray = Convert.FromBase64String(publicKey.key);
		var C = encrypt(messageByteArray, keyByteArray);
		
		// make a message json object to send to server 
		var m = new Message(C);
		m.addEmail(email);
		var messageJson = JsonConvert.SerializeObject(m, Formatting.Indented);
		
		// send the message to the server
		var client = new HttpClient();
		try {
			var request = "http://kayrun.cs.rit.edu:5000/Message/" + email;
			var content = new StringContent(messageJson, Encoding.UTF8, "application/json");
			var response = await client.PutAsync(request, content);
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException e) {
			Console.WriteLine("Error: " + e.Message);
			return;
		}
		
		// if we made it this far, the message was sent successfully 
		Console.WriteLine("Message written");
	}

	/**
	 * encrypt a message
	 * returns an encrypted, encoded message
	 *
	 * @param PArray: byte[] representing the plaintext message
	 * @param key: byte[] representing the public key
	 */
	private string encrypt(byte[] PArray, byte[] key) {
		// begin by unpacking the key 
		
		// read the first 4 bytes
		var eSizeArray = new byte[4];
		Buffer.BlockCopy(key, 0, eSizeArray, 0, 4);
		// check the endian
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(eSizeArray);
		}
		// convert these 4 bytes to e
		var e = BitConverter.ToInt32(eSizeArray, 0);
		
		// skip the first 4 bytes and read e bytes into E
		var EArray = new byte[(int)e];
		Buffer.BlockCopy(key, 4, EArray, 0, (int)e);
		// convert E into a BigInteger
		var E = new BigInteger(EArray);
		
		// skip the first 4 + e bytes and read 4 bytes into n
		var nSizeArray = new byte[4];
		Buffer.BlockCopy(key, 4 + (int)e, nSizeArray, 0, 4);
		// check the endian
		if (BitConverter.IsLittleEndian) {
			Array.Reverse(nSizeArray);
		}
		// convert these 4 bytes to n
		var n = BitConverter.ToInt32(nSizeArray, 0);
		
		// skip the first 4 + e + 4 bytes and read n bytes into N
		var NArray = new byte[(int)n];
		Buffer.BlockCopy(key, 4 + (int)e + 4, NArray, 0, (int)n);
		// convert N into a BigInteger
		var N = new BigInteger(NArray);
		
		// compute the encrypted message
		var P = new BigInteger(PArray);
		var C = BigInteger.ModPow(P, E, N);
		return Convert.ToBase64String(C.ToByteArray());
	}

}

}