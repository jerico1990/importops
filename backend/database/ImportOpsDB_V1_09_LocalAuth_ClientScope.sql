USE ImportOpsDB;
GO
SET XACT_ABORT ON;
GO

/* ============================================================================
   V10.1 - LOCAL AUTH + CLIENT SCOPE
============================================================================ */

IF COL_LENGTH('sec.APP_USER','user_type') IS NULL
    ALTER TABLE sec.APP_USER ADD user_type VARCHAR(20) NOT NULL
        CONSTRAINT DF_APP_USER_TYPE DEFAULT('INTERNAL');
GO

IF COL_LENGTH('sec.APP_USER','client_id') IS NULL
    ALTER TABLE sec.APP_USER ADD client_id BIGINT NULL;
GO

IF COL_LENGTH('sec.APP_USER','password_hash') IS NULL
    ALTER TABLE sec.APP_USER ADD password_hash NVARCHAR(500) NULL;
GO

IF COL_LENGTH('sec.APP_USER','must_change_password') IS NULL
    ALTER TABLE sec.APP_USER ADD must_change_password BIT NOT NULL
        CONSTRAINT DF_APP_USER_MUST_CHANGE DEFAULT(0);
GO

IF COL_LENGTH('sec.APP_USER','last_login_at') IS NULL
    ALTER TABLE sec.APP_USER ADD last_login_at DATETIME2(0) NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_APP_USER_CLIENT')
BEGIN
    ALTER TABLE sec.APP_USER
    ADD CONSTRAINT FK_APP_USER_CLIENT
        FOREIGN KEY(client_id) REFERENCES crm.CLIENT(client_id);
END;
GO

IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_APP_USER_TYPE_CLIENT')
BEGIN
    ALTER TABLE sec.APP_USER
    ADD CONSTRAINT CK_APP_USER_TYPE_CLIENT CHECK
    (
        (user_type='INTERNAL' AND client_id IS NULL)
        OR
        (user_type='CLIENT' AND client_id IS NOT NULL)
    );
END;
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('sec.APP_USER') AND name='IX_APP_USER_CLIENT')
    CREATE INDEX IX_APP_USER_CLIENT ON sec.APP_USER(company_id,user_type,client_id,is_active);
GO

IF COL_LENGTH('ops.OPERATION_EVENT','visibility') IS NULL
    ALTER TABLE ops.OPERATION_EVENT ADD visibility VARCHAR(10) NOT NULL
        CONSTRAINT DF_OPERATION_EVENT_VISIBILITY DEFAULT('BOTH');
GO

IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_OPERATION_EVENT_VISIBILITY')
BEGIN
    ALTER TABLE ops.OPERATION_EVENT
    ADD CONSTRAINT CK_OPERATION_EVENT_VISIBILITY CHECK(visibility IN('INTERNAL','CLIENT','BOTH'));
END;
GO

IF COL_LENGTH('doc.DOCUMENT','visibility') IS NULL
    ALTER TABLE doc.DOCUMENT ADD visibility VARCHAR(10) NOT NULL
        CONSTRAINT DF_DOCUMENT_VISIBILITY DEFAULT('BOTH');
GO

IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_DOCUMENT_VISIBILITY')
BEGIN
    ALTER TABLE doc.DOCUMENT
    ADD CONSTRAINT CK_DOCUMENT_VISIBILITY CHECK(visibility IN('INTERNAL','CLIENT','BOTH'));
END;
GO

IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='REQUESTS.CREATE')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('REQUESTS.CREATE',N'Crear y enviar solicitudes propias','REQUESTS',1);

IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='DOCUMENTS.UPLOAD')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('DOCUMENTS.UPLOAD',N'Cargar y reemplazar documentos propios','DOCUMENTS',1);
GO

DECLARE @company_id BIGINT=(SELECT TOP 1 company_id FROM cfg.COMPANY WHERE is_deleted=0 ORDER BY company_id);
IF @company_id IS NULL THROW 50001,'No existe empresa activa.',1;

/* Roles cliente */
IF NOT EXISTS(SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='CLIENT_ADMIN')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'CLIENT_ADMIN',N'Administrador Cliente',1,1);

IF NOT EXISTS(SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='CLIENT_USER')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'CLIENT_USER',N'Usuario Cliente',1,1);

/* Otorgar REQUESTS.CREATE y DOCUMENTS.UPLOAD a perfiles internos que ya gestionan esos módulos */
DECLARE @p_request_create INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='REQUESTS.CREATE');
DECLARE @p_doc_upload INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='DOCUMENTS.UPLOAD');

INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT r.role_id,@p_request_create,1,1,1,1,0
FROM sec.ROLE r
WHERE r.company_id=@company_id AND r.code IN('ADMIN','COMMERCIAL','OPERATIONS')
AND NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION rp WHERE rp.role_id=r.role_id AND rp.permission_id=@p_request_create);

INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT r.role_id,@p_doc_upload,1,1,1,0,0
FROM sec.ROLE r
WHERE r.company_id=@company_id AND r.code IN('ADMIN','COMMERCIAL','OPERATIONS')
AND NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION rp WHERE rp.role_id=r.role_id AND rp.permission_id=@p_doc_upload);

/* Permisos cliente: solo consulta propia + creación de solicitud + carga documental */
DECLARE @client_admin INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='CLIENT_ADMIN');
DECLARE @client_user INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='CLIENT_USER');

DECLARE @ClientPerm TABLE(code VARCHAR(80));
INSERT INTO @ClientPerm(code) VALUES
('CLIENTS.VIEW'),
('REQUESTS.VIEW'),
('REQUESTS.CREATE'),
('QUOTATIONS.VIEW'),
('OPERATIONS.VIEW'),
('SHIPMENTS.VIEW'),
('CUSTOMS.VIEW'),
('DOCUMENTS.VIEW'),
('DOCUMENTS.UPLOAD');

INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT role_id,p.permission_id,1,
       CASE WHEN p.code IN('REQUESTS.CREATE','DOCUMENTS.UPLOAD') THEN 1 ELSE 0 END,
       CASE WHEN p.code IN('REQUESTS.CREATE','DOCUMENTS.UPLOAD') THEN 1 ELSE 0 END,
       0,0
FROM (VALUES(@client_admin),(@client_user)) r(role_id)
JOIN sec.PERMISSION p ON p.code IN(SELECT code FROM @ClientPerm)
WHERE r.role_id IS NOT NULL
AND NOT EXISTS(
    SELECT 1 FROM sec.ROLE_PERMISSION rp
    WHERE rp.role_id=r.role_id AND rp.permission_id=p.permission_id
);
GO

/* V1.11 clean-install compatibility:
   CLIENT_ADMIN puede decidir (aprobar/rechazar) cotizaciones propias SENT. */
DECLARE @company_id_decision BIGINT =
(
    SELECT TOP 1 company_id
    FROM cfg.COMPANY
    WHERE is_deleted = 0
    ORDER BY company_id
);

DECLARE @client_admin_decision INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id_decision AND code='CLIENT_ADMIN');
DECLARE @quotation_approve_decision INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='QUOTATIONS.APPROVE');

IF @client_admin_decision IS NOT NULL
   AND @quotation_approve_decision IS NOT NULL
   AND NOT EXISTS(
       SELECT 1 FROM sec.ROLE_PERMISSION
       WHERE role_id=@client_admin_decision
         AND permission_id=@quotation_approve_decision
   )
BEGIN
    INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
    VALUES(@client_admin_decision,@quotation_approve_decision,1,0,0,0,1);
END;
GO

/* ============================================================================
   CLIENTES DEMO PARA PROBAR AISLAMIENTO
============================================================================ */
DECLARE @company BIGINT=(SELECT TOP 1 company_id FROM cfg.COMPANY WHERE is_deleted=0 ORDER BY company_id);

IF NOT EXISTS(SELECT 1 FROM crm.CLIENT WHERE company_id=@company AND tax_id='20600000001')
BEGIN
    INSERT INTO crm.CLIENT(company_id,client_type,legal_name,trade_name,tax_id,country_code,email,phone,preferred_currency_code,is_active,created_at,is_deleted)
    VALUES(@company,'COMPANY',N'Cliente Demo Andino SAC',N'Andino Demo','20600000001','PE',N'contacto@andino-demo.pe',N'999111111','USD',1,SYSUTCDATETIME(),0);
END;

IF NOT EXISTS(SELECT 1 FROM crm.CLIENT WHERE company_id=@company AND tax_id='20600000002')
BEGIN
    INSERT INTO crm.CLIENT(company_id,client_type,legal_name,trade_name,tax_id,country_code,email,phone,preferred_currency_code,is_active,created_at,is_deleted)
    VALUES(@company,'COMPANY',N'Cliente Demo Pacífico SAC',N'Pacífico Demo','20600000002','PE',N'contacto@pacifico-demo.pe',N'999222222','USD',1,SYSUTCDATETIME(),0);
END;

DECLARE @client_a BIGINT=(SELECT client_id FROM crm.CLIENT WHERE company_id=@company AND tax_id='20600000001');
DECLARE @client_b BIGINT=(SELECT client_id FROM crm.CLIENT WHERE company_id=@company AND tax_id='20600000002');

DECLARE @admin_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='ADMIN');
DECLARE @commercial_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='COMMERCIAL');
DECLARE @operations_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='OPERATIONS');
DECLARE @finance_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='FINANCE');
DECLARE @client_admin_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='CLIENT_ADMIN');
DECLARE @client_user_role INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company AND code='CLIENT_USER');

DECLARE @Users TABLE(
 username NVARCHAR(100), email NVARCHAR(200), first_name NVARCHAR(100), last_name NVARCHAR(100),
 user_type VARCHAR(20), client_id BIGINT NULL, password_hash NVARCHAR(500), role_id INT
);

INSERT INTO @Users VALUES
(N'admin',N'admin@importops.local',N'Administrador',N'ImportOps','INTERNAL',NULL,N'PBKDF2-SHA256$100000$ap0n6TQ0AomxkcFpi9IpKw==$RJGBiFpCqdHz7sp6EkGIERwJTc0u7QME3trv/xHn4Lo=',@admin_role),
(N'comercial.demo',N'comercial@importops.local',N'Usuario',N'Comercial','INTERNAL',NULL,N'PBKDF2-SHA256$100000$eTQy82xYz836rB6iBppzag==$olELppqYFlAS2uayA1CmZi6++VL/wX3qYUUKFMAtDWg=',@commercial_role),
(N'operaciones.demo',N'operaciones@importops.local',N'Usuario',N'Operaciones','INTERNAL',NULL,N'PBKDF2-SHA256$100000$TAGGXVZ4f+cVNoz4Xa8fcA==$Qo+YAvNct2GLs12vext0eMAxlvxQcevxcKhrHA3nNgA=',@operations_role),
(N'finanzas.demo',N'finanzas@importops.local',N'Usuario',N'Finanzas','INTERNAL',NULL,N'PBKDF2-SHA256$100000$rqOwwyzF2FBEZxGvoEIIDQ==$PRCMP2baaU9xP7xK0New8apf8xfNvzVs/wlNrzMrY/A=',@finance_role),
(N'cliente.admin.andino',N'admin@andino-demo.pe',N'Administrador',N'Andino','CLIENT',@client_a,N'PBKDF2-SHA256$100000$Hrc1VCL7SNvU7+ODHaG6Gw==$82A6xxG8yeL5YFyD6SFVzlBDDQCCNGPQZp90J7ACaxI=',@client_admin_role),
(N'cliente.usuario.andino',N'usuario@andino-demo.pe',N'Usuario',N'Andino','CLIENT',@client_a,N'PBKDF2-SHA256$100000$W4BjNxnpocBD83CW+rxczA==$649Jdm5EuMCWP8R1kg7Nw0GJLTBLEtHW5/VLaell360=',@client_user_role),
(N'cliente.usuario.pacifico',N'usuario@pacifico-demo.pe',N'Usuario',N'Pacífico','CLIENT',@client_b,N'PBKDF2-SHA256$100000$EGv8Ql1QOAbfRTZqcsybaw==$VT1SygfS9p4ntmYQ30C2woP3smD0kuQEkD74FpGtOds=',@client_user_role);

DECLARE @username NVARCHAR(100),@email NVARCHAR(200),@first NVARCHAR(100),@last NVARCHAR(100),
        @user_type VARCHAR(20),@client_id BIGINT,@password_hash NVARCHAR(500),@role_id INT,@user_id BIGINT;

DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT username,email,first_name,last_name,user_type,client_id,password_hash,role_id FROM @Users;

OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @username,@email,@first,@last,@user_type,@client_id,@password_hash,@role_id;

WHILE @@FETCH_STATUS=0
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sec.APP_USER WHERE company_id=@company AND username=@username AND is_deleted=0)
    BEGIN
        INSERT INTO sec.APP_USER(
            company_id,username,email,first_name,last_name,user_type,client_id,password_hash,
            must_change_password,is_active,created_at,is_deleted
        )
        VALUES(
            @company,@username,@email,@first,@last,@user_type,@client_id,@password_hash,
            0,1,SYSUTCDATETIME(),0
        );
    END
    ELSE
    BEGIN
        UPDATE sec.APP_USER
        SET email=@email,first_name=@first,last_name=@last,user_type=@user_type,client_id=@client_id,
            password_hash=@password_hash,is_active=1
        WHERE company_id=@company AND username=@username AND is_deleted=0;
    END;

    SELECT @user_id=user_id FROM sec.APP_USER WHERE company_id=@company AND username=@username AND is_deleted=0;

    IF @role_id IS NOT NULL AND NOT EXISTS(
        SELECT 1 FROM sec.USER_ROLE WHERE user_id=@user_id AND role_id=@role_id
    )
        INSERT INTO sec.USER_ROLE(user_id,role_id,assigned_at,assigned_by)
        VALUES(@user_id,@role_id,SYSUTCDATETIME(),NULL);

    FETCH NEXT FROM user_cursor INTO @username,@email,@first,@last,@user_type,@client_id,@password_hash,@role_id;
END;

CLOSE user_cursor;
DEALLOCATE user_cursor;
GO

SELECT
    u.user_id,
    u.username,
    u.user_type,
    c.legal_name AS client_name,
    r.code AS role_code
FROM sec.APP_USER u
LEFT JOIN crm.CLIENT c ON c.client_id=u.client_id
LEFT JOIN sec.USER_ROLE ur ON ur.user_id=u.user_id
LEFT JOIN sec.ROLE r ON r.role_id=ur.role_id
WHERE u.username IN(
    'admin','comercial.demo','operaciones.demo','finanzas.demo',
    'cliente.admin.andino','cliente.usuario.andino','cliente.usuario.pacifico'
)
ORDER BY u.user_type,u.username;
GO
