BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "_Message" (
	"DateAndTime"	INTEGER,
	"Sender"	TEXT,
	"Recipient"	TEXT,
	"Message"	TEXT,
	PRIMARY KEY("DateAndTime","Sender","Recipient"),
	FOREIGN KEY("Sender") REFERENCES "_User"("Username"),
	FOREIGN KEY("Recipient") REFERENCES "_User"("Username")
);
CREATE TABLE IF NOT EXISTS "_Friend" (
	"Friend1"	TEXT,
	"Friend2"	TEXT,
	PRIMARY KEY("Friend1","Friend2"),
	FOREIGN KEY("Friend2") REFERENCES "_User"("Username"),
	FOREIGN KEY("Friend1") REFERENCES "_User"("Username")
);
CREATE TABLE IF NOT EXISTS "_User" (
	"Username"	TEXT,
	"FirstName"	TEXT,
	"Surname"	TEXT,
	"Passwd"	BLOB,
	"ClientTextSize"	INTEGER,
	"ClientColour"	TEXT,
	PRIMARY KEY("Username")
);
COMMIT;
