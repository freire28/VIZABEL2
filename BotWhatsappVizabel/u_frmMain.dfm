object Form1: TForm1
  Left = 0
  Top = 0
  Caption = 'Form1'
  ClientHeight = 517
  ClientWidth = 737
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  OnClose = FormClose
  OnShow = FormShow
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 737
    Height = 41
    Align = alTop
    Color = 16744448
    ParentBackground = False
    TabOrder = 0
    ExplicitWidth = 733
    object SpeedButton1: TSpeedButton
      AlignWithMargins = True
      Left = 4
      Top = 4
      Width = 109
      Height = 33
      Align = alLeft
      Caption = 'Whatsapp'
      Flat = True
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWhite
      Font.Height = -13
      Font.Name = 'Tahoma'
      Font.Style = [fsBold]
      ParentFont = False
      OnClick = SpeedButton1Click
    end
  end
  object Inject1: TInject
    InjectJS.AutoUpdateTimeOut = 30
    Config.AutoDelay = 1000
    AjustNumber.LengthPhone = 8
    AjustNumber.DDIDefault = 55
    FormQrCodeType = Ft_Http
    OnGetUnReadMessages = Inject1GetUnReadMessages
    Left = 32
    Top = 56
  end
  object rstclntApiBoot: TRESTClient
    Accept = 'application/json, text/plain; q=0.9, text/html;q=0.8,'
    AcceptCharset = 'utf-8, *;q=0.8'
    BaseURL = 'https://localhost:57428/enviarmensagem'
    ContentType = 'application/json'
    Params = <>
    SynchronizedEvents = False
    Left = 72
    Top = 64
  end
  object rstrqstApiBoot: TRESTRequest
    AssignedValues = [rvConnectTimeout, rvReadTimeout]
    Client = rstclntApiBoot
    Method = rmPOST
    Params = <
      item
        Kind = pkREQUESTBODY
        Name = 'body33CFE30D516C411A9998877FB641DA8D'
        Value = 
          '{'#13#10'  "idContato": "010101asdfasd101",'#13#10'  "nomecontato": "denis",' +
          #13#10'  "mensagem": "oi"'#13#10'}'#13#10
        ContentTypeStr = 'application/json'
      end>
    Response = rstrspnsApiBoot
    SynchronizedEvents = False
    Left = 144
    Top = 64
  end
  object rstrspnsApiBoot: TRESTResponse
    ContentType = 'application/json'
    Left = 104
    Top = 64
  end
  object FDConConexao: TFDConnection
    Params.Strings = (
      'Database=VIZABEL'
      'User_Name=usuariovizabel'
      'Server=191.252.221.249'
      'Password=@Vizabel123'
      'DriverID=MSSQL')
    Connected = True
    LoginPrompt = False
    Left = 48
    Top = 120
  end
  object fdqryFDQConfiguracoes: TFDQuery
    Connection = FDConConexao
    SQL.Strings = (
      'select * from CONFIGURACOES WHERE CHAVE = :CHAVE')
    Left = 88
    Top = 120
    ParamData = <
      item
        Name = 'CHAVE'
        DataType = ftString
        ParamType = ptInput
      end>
  end
end
