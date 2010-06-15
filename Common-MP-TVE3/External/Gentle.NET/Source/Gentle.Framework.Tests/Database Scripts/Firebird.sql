/******************************************************************************/
/****         Generated by IBExpert 2005.09.25 28.12.2005 14:45:34         ****/
/******************************************************************************/

SET SQL DIALECT 3;

SET NAMES UNICODE_FSS;

/*
CREATE DATABASE 'localhost:D:\GENTLE_FIREBIRD.FDB'
USER 'SYSDBA'
PASSWORD 'masterkey'
PAGE_SIZE 4096
DEFAULT CHARACTER SET UNICODE_FSS;
*/



/******************************************************************************/
/****                               Domains                                ****/
/******************************************************************************/

CREATE DOMAIN GUID AS
VARCHAR(36);

CREATE DOMAIN TEXT AS
VARCHAR(1000);



/******************************************************************************/
/****                              Generators                              ****/
/******************************************************************************/

CREATE GENERATOR LISTMEMBER_SEQ;
SET GENERATOR LISTMEMBER_SEQ TO 19;

CREATE GENERATOR LIST_SEQ;
SET GENERATOR LIST_SEQ TO 8080;

CREATE GENERATOR MEMBERPICTURE_SEQ;
SET GENERATOR MEMBERPICTURE_SEQ TO 0;

CREATE GENERATOR MULTITYPE_SEQ;
SET GENERATOR MULTITYPE_SEQ TO 0;

CREATE GENERATOR PROPERTYHOLDER_SEQ;
SET GENERATOR PROPERTYHOLDER_SEQ TO 16;

CREATE GENERATOR ROLES_SEQ;
SET GENERATOR ROLES_SEQ TO 80;

CREATE GENERATOR USERS_SEQ;
SET GENERATOR USERS_SEQ TO 20;



/******************************************************************************/
/****                                Tables                                ****/
/******************************************************************************/




CREATE TABLE GUIDHOLDER (
    GUID       GUID NOT NULL,
    SOMEVALUE  INTEGER NOT NULL
);

CREATE TABLE LIST (
    LISTID         INTEGER NOT NULL,
    LISTNAME       TEXT NOT NULL,
    SENDERADDRESS  VARCHAR(255) NOT NULL
);

CREATE TABLE LISTMEMBER (
    MEMBERID         INTEGER NOT NULL,
    LISTID           INTEGER NOT NULL,
    MEMBERNAME       TEXT,
    MEMBERADDRESS    VARCHAR(255) NOT NULL,
    DATABASEVERSION  INTEGER DEFAULT 1 NOT NULL
);

CREATE TABLE MEMBERPICTURE (
    PICTUREID    INTEGER NOT NULL,
    MEMBERID     INTEGER NOT NULL,
    PICTUREDATA  BLOB SUB_TYPE 0 SEGMENT SIZE 80 NOT NULL
);

CREATE TABLE MULTITYPE (
    ID      INTEGER NOT NULL,
    "TYPE"   VARCHAR(250) NOT NULL,
    FIELD1  INTEGER,
    FIELD2  DECIMAL(15,8),
    FIELD3  FLOAT,
    FIELD4  TEXT
);

CREATE TABLE PROPERTYHOLDER (
    PH_ID        INTEGER NOT NULL,
    PH_NAME      TEXT NOT NULL,
    TINT         INTEGER,
    TLONG        BIGINT,
    TDECIMAL     DECIMAL(15,2),
    TDOUBLE      DOUBLE PRECISION,
    TBOOL        SMALLINT,
    TDATETIME    TIMESTAMP,
    TDATETIMENN  TIMESTAMP NOT NULL,
    TCHAR        CHAR(8),
    TNCHAR       CHAR(8),
    TVARCHAR     VARCHAR(8),
    TNVARCHAR    VARCHAR(8),
    TTEXT        BLOB SUB_TYPE 1 SEGMENT SIZE 80,
    TNTEXT       BLOB SUB_TYPE 1 SEGMENT SIZE 80
);

CREATE TABLE ROLES (
    ROLEID    INTEGER NOT NULL,
    ROLENAME  TEXT NOT NULL
);

CREATE TABLE USERROLES (
    USERID    INTEGER NOT NULL,
    ROLEID    INTEGER NOT NULL,
    MEMBERID  INTEGER
);

CREATE TABLE USERS (
    USERID       INTEGER NOT NULL,
    FIRSTNAME    VARCHAR(255) NOT NULL,
    LASTNAME     VARCHAR(255) NOT NULL,
    PRIMARYROLE  INTEGER NOT NULL
);

INSERT INTO LIST (LISTID, LISTNAME, SENDERADDRESS) VALUES (1, 'Announce', 'ann-sender@foobar.com');
INSERT INTO LIST (LISTID, LISTNAME, SENDERADDRESS) VALUES (2, 'Discussion', 'first.foobar.com');
INSERT INTO LIST (LISTID, LISTNAME, SENDERADDRESS) VALUES (3, 'Messages', 'info-sender@foobar.org');
INSERT INTO LIST (LISTID, LISTNAME, SENDERADDRESS) VALUES (8133, 'test', 'test@test.com');

COMMIT WORK;

INSERT INTO LISTMEMBER (MEMBERID, LISTID, MEMBERNAME, MEMBERADDRESS, DATABASEVERSION) VALUES (1, 1, 'First User', 'first@foobar.com', 1);
INSERT INTO LISTMEMBER (MEMBERID, LISTID, MEMBERNAME, MEMBERADDRESS, DATABASEVERSION) VALUES (2, 2, 'First User', 'first@foobar.com', 1);
INSERT INTO LISTMEMBER (MEMBERID, LISTID, MEMBERNAME, MEMBERADDRESS, DATABASEVERSION) VALUES (3, 1, 'Second User', 'second@bar.com', 1);
INSERT INTO LISTMEMBER (MEMBERID, LISTID, MEMBERNAME, MEMBERADDRESS, DATABASEVERSION) VALUES (4, 3, 'Third User', 'third@foo.org', 1);

COMMIT WORK;



/******************************************************************************/
/****                             Primary Keys                             ****/
/******************************************************************************/

ALTER TABLE GUIDHOLDER ADD CONSTRAINT PK_GUIDHOLDER PRIMARY KEY (GUID);
ALTER TABLE LIST ADD CONSTRAINT PK_LIST PRIMARY KEY (LISTID);
ALTER TABLE LISTMEMBER ADD CONSTRAINT PK_LISTMEMBER PRIMARY KEY (MEMBERID);
ALTER TABLE MEMBERPICTURE ADD CONSTRAINT PK_MEMBERPICTURE PRIMARY KEY (PICTUREID);
ALTER TABLE MULTITYPE ADD CONSTRAINT PK_MULTITYPE PRIMARY KEY (ID);
ALTER TABLE PROPERTYHOLDER ADD CONSTRAINT PK_PROPERTYHOLDER PRIMARY KEY (PH_ID);
ALTER TABLE ROLES ADD CONSTRAINT PK_ROLES PRIMARY KEY (ROLEID);
ALTER TABLE USERROLES ADD CONSTRAINT PK_USERROLES PRIMARY KEY (USERID, ROLEID);
ALTER TABLE USERS ADD CONSTRAINT PK_USERS PRIMARY KEY (USERID);


/******************************************************************************/
/****                             Foreign Keys                             ****/
/******************************************************************************/

ALTER TABLE LISTMEMBER ADD CONSTRAINT FK_LISTMEMBER_LIST FOREIGN KEY (LISTID) REFERENCES LIST (LISTID) ON DELETE CASCADE ON UPDATE CASCADE
  USING INDEX FK_LISTMEMBER;
ALTER TABLE MEMBERPICTURE ADD CONSTRAINT FK_MEMBERPICTURE_LISTMEMBER FOREIGN KEY (MEMBERID) REFERENCES LISTMEMBER (MEMBERID) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE USERROLES ADD CONSTRAINT FK_USERROLES_ROLES FOREIGN KEY (ROLEID) REFERENCES ROLES (ROLEID) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE USERROLES ADD CONSTRAINT FK_USERROLES_USERS FOREIGN KEY (USERID) REFERENCES USERS (USERID) ON DELETE CASCADE ON UPDATE CASCADE;


/******************************************************************************/
/****                               Triggers                               ****/
/******************************************************************************/


SET TERM ^ ;



/* Trigger: LISTMEMBER_BI */
CREATE TRIGGER LISTMEMBER_BI FOR LISTMEMBER
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.MEMBERID IS NULL) THEN
    NEW.MEMBERID = GEN_ID(LISTMEMBER_SEQ,1);
END
^

/* Trigger: LIST_BI */
CREATE TRIGGER LIST_BI FOR LIST
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.LISTID IS NULL) THEN
    NEW.LISTID = GEN_ID(LIST_SEQ,1);
END
^

/* Trigger: MEMBERPICTURE_BI */
CREATE TRIGGER MEMBERPICTURE_BI FOR MEMBERPICTURE
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.PICTUREID IS NULL) THEN
    NEW.PICTUREID = GEN_ID(MEMBERPICTURE_SEQ,1);
END
^

/* Trigger: MULTITYPE_BI */
CREATE TRIGGER MULTITYPE_BI FOR MULTITYPE
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID IS NULL) THEN
    NEW.ID = GEN_ID(MULTITYPE_SEQ,1);
END
^

/* Trigger: PROPERTYHOLDER_BI */
CREATE TRIGGER PROPERTYHOLDER_BI FOR PROPERTYHOLDER
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.PH_ID IS NULL) THEN
    NEW.PH_ID = GEN_ID(PROPERTYHOLDER_SEQ,1);
END
^

/* Trigger: ROLES_BI */
CREATE TRIGGER ROLES_BI FOR ROLES
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ROLEID IS NULL) THEN
    NEW.ROLEID = GEN_ID(ROLES_SEQ,1);
END
^

/* Trigger: USERS_BI */
CREATE TRIGGER USERS_BI FOR USERS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.USERID IS NULL) THEN
    NEW.USERID = GEN_ID(USERS_SEQ,1);
END
^

SET TERM ; ^
