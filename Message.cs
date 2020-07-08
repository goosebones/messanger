/*
 * Class to represent Messages in the messaging program.
 *
 * @author Gunther Kroth   gdk6217@rit.edu
 * @file Message.cs
 */

namespace Messenger {

/*
 * class to represent a message
 * a Message can be sent to and retrieved from the server
 */
class Message {
	
	// a Message will have one email assigned to it
	public string email { get; private set; }
	// message content will be stored as a Base64 encoded string
	public string content { get; private set; }

	/*
	 * create a new Message
	 * email field is for the intended recipient user
	 *
	 * @param content: Base64 encoded string containing an encrypted message
	 */
	public Message(string content) {
		// email is initialized to an empty string
		this.email = "";
		this.content = content;
	}

	/*
	 * assign an email address to this Message
	 * if an email for this Message already exists, it will be overwitten
	 *
	 * @param email: email string to store
	 */
	public void addEmail(string email) {
		this.email = email;
	}

}

}