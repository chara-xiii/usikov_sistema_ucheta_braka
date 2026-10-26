/* ============================================================
   Учёт брака и прогнозирование закупок качественного товара
   Вариант 24. Скрипт создания таблиц (префикс UP03_)
   ============================================================ */

-- Select the target database before running this script.
-- USE YOUR_DATABASE;
GO

/* Удаляем таблицы в порядке, обратном зависимостям */
IF OBJECT_ID('UP03_Spisanie', 'U')     IS NOT NULL DROP TABLE UP03_Spisanie;
IF OBJECT_ID('UP03_Zayavki', 'U')      IS NOT NULL DROP TABLE UP03_Zayavki;
IF OBJECT_ID('UP03_Brak', 'U')         IS NOT NULL DROP TABLE UP03_Brak;
IF OBJECT_ID('UP03_Partii', 'U')       IS NOT NULL DROP TABLE UP03_Partii;
IF OBJECT_ID('UP03_Materialy', 'U')    IS NOT NULL DROP TABLE UP03_Materialy;
IF OBJECT_ID('UP03_Postavshchiki', 'U')IS NOT NULL DROP TABLE UP03_Postavshchiki;
IF OBJECT_ID('UP03_Polzovateli', 'U')  IS NOT NULL DROP TABLE UP03_Polzovateli;
IF OBJECT_ID('UP03_Roli', 'U')         IS NOT NULL DROP TABLE UP03_Roli;
GO

/* Роли пользователей */
CREATE TABLE UP03_Roli
(
    Id_roli         INT IDENTITY(1,1) NOT NULL,
    Nazvanie_roli   NVARCHAR(50)      NOT NULL,
    Opisanie        NVARCHAR(200)     NULL,
    CONSTRAINT PK_UP03_Roli PRIMARY KEY (Id_roli)
);
GO

/* Пользователи системы */
CREATE TABLE UP03_Polzovateli
(
    Id_polzovatelya   INT IDENTITY(1,1) NOT NULL,
    FIO               NVARCHAR(150)     NOT NULL,
    Login             NVARCHAR(50)      NOT NULL,
    Parol             NVARCHAR(50)      NOT NULL,
    Id_roli           INT               NOT NULL,
    Data_registracii  DATE              NOT NULL CONSTRAINT DF_UP03_Polz_Date DEFAULT (GETDATE()),
    Aktiven           BIT               NOT NULL CONSTRAINT DF_UP03_Polz_Aktiven DEFAULT (1),
    CONSTRAINT PK_UP03_Polzovateli PRIMARY KEY (Id_polzovatelya),
    CONSTRAINT UQ_UP03_Polzovateli_Login UNIQUE (Login),
    CONSTRAINT FK_UP03_Polzovateli_Roli FOREIGN KEY (Id_roli) REFERENCES UP03_Roli (Id_roli)
);
GO

/* Справочник поставщиков */
CREATE TABLE UP03_Postavshchiki
(
    Id_postavshchika  INT IDENTITY(1,1) NOT NULL,
    Naimenovanie      NVARCHAR(150)     NOT NULL,
    INN               NVARCHAR(12)      NULL,
    Telefon           NVARCHAR(30)      NULL,
    Email             NVARCHAR(100)     NULL,
    Adres             NVARCHAR(200)     NULL,
    Data_dogovora     DATE              NULL,
    CONSTRAINT PK_UP03_Postavshchiki PRIMARY KEY (Id_postavshchika)
);
GO

/* Справочник материалов */
CREATE TABLE UP03_Materialy
(
    Id_materiala        INT IDENTITY(1,1) NOT NULL,
    Naimenovanie        NVARCHAR(150)     NOT NULL,
    Edinica_izmereniya  NVARCHAR(20)      NULL,
    Tip                 NVARCHAR(50)      NULL,
    Cena                DECIMAL(18,2)     NULL,
    CONSTRAINT PK_UP03_Materialy PRIMARY KEY (Id_materiala)
);
GO

/* Партии поступления материалов */
CREATE TABLE UP03_Partii
(
    Id_partii          INT IDENTITY(1,1) NOT NULL,
    Id_postavshchika   INT               NOT NULL,
    Id_materiala       INT               NOT NULL,
    Data_postupleniya  DATE              NOT NULL,
    Kolichestvo        DECIMAL(18,2)     NOT NULL,
    Cena_postavki      DECIMAL(18,2)     NULL,
    Nomer_nakladnoi    NVARCHAR(30)      NULL,
    CONSTRAINT PK_UP03_Partii PRIMARY KEY (Id_partii),
    CONSTRAINT FK_UP03_Partii_Postavshchiki FOREIGN KEY (Id_postavshchika) REFERENCES UP03_Postavshchiki (Id_postavshchika),
    CONSTRAINT FK_UP03_Partii_Materialy    FOREIGN KEY (Id_materiala)     REFERENCES UP03_Materialy (Id_materiala)
);
GO

/* Учёт брака */
CREATE TABLE UP03_Brak
(
    Id_braka            INT IDENTITY(1,1) NOT NULL,
    Id_partii           INT               NULL,
    Id_materiala        INT               NOT NULL,
    Id_polzovatelya     INT               NULL,
    Data_vyyavleniya    DATE              NOT NULL,
    Kolichestvo         DECIMAL(18,2)     NOT NULL,
    Prichina            NVARCHAR(200)     NULL,
    Opisanie            NVARCHAR(500)     NULL,
    Status              NVARCHAR(30)      NOT NULL CONSTRAINT DF_UP03_Brak_Status DEFAULT (N'Зарегистрирован'),
    CONSTRAINT PK_UP03_Brak PRIMARY KEY (Id_braka),
    CONSTRAINT FK_UP03_Brak_Partii        FOREIGN KEY (Id_partii)       REFERENCES UP03_Partii (Id_partii),
    CONSTRAINT FK_UP03_Brak_Materialy     FOREIGN KEY (Id_materiala)    REFERENCES UP03_Materialy (Id_materiala),
    CONSTRAINT FK_UP03_Brak_Polzovateli   FOREIGN KEY (Id_polzovatelya) REFERENCES UP03_Polzovateli (Id_polzovatelya)
);
GO

/* Списание брака */
CREATE TABLE UP03_Spisanie
(
    Id_spisaniya      INT IDENTITY(1,1) NOT NULL,
    Id_braka          INT               NOT NULL,
    Id_polzovatelya   INT               NULL,
    Data_spisaniya    DATE              NOT NULL,
    Kolichestvo       DECIMAL(18,2)     NOT NULL,
    Prichina          NVARCHAR(200)     NULL,
    CONSTRAINT PK_UP03_Spisanie PRIMARY KEY (Id_spisaniya),
    CONSTRAINT FK_UP03_Spisanie_Brak        FOREIGN KEY (Id_braka)        REFERENCES UP03_Brak (Id_braka),
    CONSTRAINT FK_UP03_Spisanie_Polzovateli FOREIGN KEY (Id_polzovatelya) REFERENCES UP03_Polzovateli (Id_polzovatelya)
);
GO

/* Заявки производства на материалы */
CREATE TABLE UP03_Zayavki
(
    Id_zayavki        INT IDENTITY(1,1) NOT NULL,
    Id_materiala      INT               NOT NULL,
    Id_polzovatelya   INT               NULL,
    Data_zayavki      DATE              NOT NULL,
    Kolichestvo       DECIMAL(18,2)     NOT NULL,
    Kommentarii       NVARCHAR(300)     NULL,
    Status            NVARCHAR(30)      NOT NULL CONSTRAINT DF_UP03_Zayavki_Status DEFAULT (N'Новая'),
    CONSTRAINT PK_UP03_Zayavki PRIMARY KEY (Id_zayavki),
    CONSTRAINT FK_UP03_Zayavki_Materialy    FOREIGN KEY (Id_materiala)    REFERENCES UP03_Materialy (Id_materiala),
    CONSTRAINT FK_UP03_Zayavki_Polzovateli  FOREIGN KEY (Id_polzovatelya) REFERENCES UP03_Polzovateli (Id_polzovatelya)
);
GO
