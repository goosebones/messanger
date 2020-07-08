/*
 * Classes to represent Keys in the messaging program.
 *
 * @author Gunther Kroth   gdk6217@rit.edu
 * @file Key.cs
 */
using System.Collections.Generic;

namespace Messenger {

/*
 * class to represent a user's Private Key
 */
class PrivateKey {
	
	// multiple emails can be tied to one private key 
	public List<string> email { get; private set; }
	// keys will be stored in as a Base64 encoded string
	public string key { get; private set; }

	/*
	 * create new Private Key
	 *
	 * @param key: Base64 encoded key
	 */
	public PrivateKey(string key) {
		// email list is initialized to empty
		this.email = new List<string>();
		this.key = key;
	}

	/*
	 * assign an email address to this Private Key
	 * if the email address is already assigned, it is ignored
	 *
	 * @param email: email string
	 */
	public void addEmail(string email) {
		if (!this.email.Contains(email)) {
			this.email.Add(email);
		}
	}

}

/*
 * class to represent a user's Public Key 
 */
class PublicKey {
	
	// a Public Key will have one email assigned to it
	public string email { get; private set; }
	// keys will be stored as a Base64 encoded string
	public string key { get; private set; }

	/*
	 * create a new Public key
	 *
	 * @param key: Base63 encoded key
	 */
	public PublicKey(string key) {
		// email is initialized to an empty string
		this.email = "";
		this.key = key;
	}

	/*
	 * assign an email address to this Public Key
	 * if an email for this Public Key already exists, it will be overwritten
	 *
	 * @param email: email string to store
	 */
	public void addEmail(string email) {
		this.email = email;
	}

}

}