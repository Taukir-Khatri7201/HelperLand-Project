# Note 
This note is regarging the change in one field of User table. In particular, the Password field.

To encrypt the password provided by the user while registering, I have used data protector which encrypts the password in HASH format having length greater than the provided length in the schema.

Please change the required size at your side if needed.