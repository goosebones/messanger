/*
 * Program to send secure messages to other users.
 * Uses Public key Encryption to send messages using email addresses.
 *
 * @author Gunther Kroth   gdk6217@rit.edu
 * @file Program.cs
 */

using System;
using System.Threading.Tasks;

namespace Messenger {

/*
 * class to enforce correct usage of this program
 */
class Usage {
	/*
	 * print the usage message for this program
	 * called when a user enters invalid arguments
	 */
	public void printUsage() {
		Console.WriteLine("Usage: Messenger <option> <other arguments>\n" +
		                  "Available options:\n" +
		                  "keyGen <keysize>\tgenerate a public and private key pair\n" +
		                  "\t\t\tthese will be stored locally on the disk\n" +
		                  "sendKey <email>\t\tsend the public key to the server and associate it with the email address\n" +
		                  "\t\t\tupdates local system and registers the email address\n" +
		                  "getKey <email>\t\tretrieve the public key associated with the email address\n" +
		                  "sendMsg <email> <text>\tsend the plaintext message to the email address\n" +
		                  "getMsg <email>\t\tretrieve and print a message from the email address\n" +
		                  "\t\t\tif the private key of the email address is unknown, the message can't be decoded");
	}

	/*
	 * print a message that informs the user of invalid keysize
	 * called when the keyGen mode is tried with bad arguments
	 */
	public void invalidKeyGen() {
		Console.WriteLine("keyGen takes a positive integer as an argument.\n");
		printUsage();
	}
	
	/*
	 * print a message that informs the user of an invalid email argument
	 * called when sendKey, getKey, or getMsg are tried with bad arguments
	 *
	 * @param mode: running mode supplied by user
	 */
	public void invalidEmail(Input.Mode mode) {
		Console.WriteLine(mode.ToString() + " takes an email address as an argument.\n");
		printUsage();
	}

	/*
	 * print a message that informs the user of invalid sendMsg mode
	 * called wen the sendMsg mode is tried with bad arguments
	 */
	public void invalidSendMsg() {
		Console.WriteLine("sendMsg takes an email address and a message as arguments.\n");
		printUsage();
	}
	
}

/*
 * class to handle command line input from the user
 */
class Input {

	/*
	 * specify the action to perform
	 * keyGen - generate a key pair
	 * sendKey - send public key to server
	 * getKey - retrieve public key from server
	 * sendMsg - send a message to a user
	 * getMsg - retrieve a message from user
	 */
	public enum Mode {
		keyGen,
		sendKey,
		getKey,
		sendMsg,
		getMsg
	}

	// user provided mode 
	public Mode mode { get; private set; }

	// these will be copied from command line 
	public string[] args { get; private set; }


	/*
	 * parse command line arguments
	 *
	 * @param args: command line arguments
	 * @return true if arguments are acceptable, false otherwise
	 */
	public bool checkInput(string[] args) {
		// new usage instance to print messages
		var usage = new Usage();

		// check for correct number of args
		if (args.Length < 1) {
			usage.printUsage();
			return false;
		}

		// check for mode
		switch (args[0]) {
			case "keyGen":
				this.mode = Mode.keyGen;
				break;
			case "sendKey":
				this.mode = Mode.sendKey;
				break;
			case "getKey":
				this.mode = Mode.getKey;
				break;
			case "sendMsg":
				this.mode = Mode.sendMsg;
				break;
			case "getMsg":
				this.mode = Mode.getMsg;
				break;
			default:
				usage.printUsage();
				return false;
		}

		// check for mode arguments
		this.args = new string[2];
		this.args[0] = this.args[1] = ".";
		// keyGen takes 1 integer
		if (this.mode == Mode.keyGen) {
			// check if user supplied a keySize
			if (args.Length != 2) {
				usage.invalidKeyGen();
				return false;
			}
			// check that keySize is a valid integer
			var keySize = 0;
			var isNumber = Int32.TryParse(args[1], out keySize);
			if (!isNumber || keySize < 1) {
				usage.invalidKeyGen();
				return false;
			}
			// if we made it this far, keySize is good
			this.args[0] = args[1];
			return true;

		}

		// sendKey, getKey, and getMsg take 1 email address string
		if (this.mode == Mode.sendKey || this.mode == Mode.getKey || this.mode == Mode.getMsg) {
			// check if user supplied an email 
			if (args.Length != 2) {
				usage.invalidEmail(this.mode);
				return false;
			}
			// check for valid email address
			if (!args[1].Contains('@')) {
				usage.invalidEmail(this.mode);
				return false;
			}
			// if we made it this far, email is good
			this.args[0] = args[1];
			return true;
		}

		// sendMsg takes an email address string and a message to send
		if (this.mode == Mode.sendMsg) {
			// check if user supplied an email and message
			if (args.Length != 3) {
				usage.invalidSendMsg();
				return false;
			}
			// check for valid email address
			if (!args[1].Contains('@')) {
				usage.invalidSendMsg();
				return false;
			}
			// if we made it this far, email and message are good
			this.args[0] = args[1];
			this.args[1] = args[2];
			return true;
		}

		// if we fell all the way through each mode, something went wrong
		usage.printUsage();
		return false;
	}

}


/*
 * main class to run program
 */
class Program {
	/*
	 * main method to run program
	 *
	 * @param args command line arguments
	 */
	static async Task Main(string[] args) {
		// first check for correct input
		var input = new Input();
		if (!input.checkInput(args)) {
			// passing 2 indicates file is in wrong format
			Environment.Exit(2);
		}
		
		// choose what action to do
		switch (input.mode) {
			// generate key
			case Input.Mode.keyGen:
				var keyGen = new KeyGen();
				keyGen.start(Int32.Parse(input.args[0]));
				break;
			// send key 
			case Input.Mode.sendKey:
				var sendKey = new SendKey();
				await sendKey.start(input. args[0]);
				break;
			// get key
			case Input.Mode.getKey:
				var getKey = new GetKey();
				await getKey.start(input.args[0]);
				break;
			// send message
			case Input.Mode.sendMsg:
				var sendMsg = new SendMsg();
				await sendMsg.start(input.args[0], input.args[1]);
				break;
			// get message
			case Input.Mode.getMsg:
				var getMsg = new GetMsg();
				await getMsg.start(input.args[0]);
				break;
			default:
				Environment.Exit(2);
				break;
		}
	}
}

}