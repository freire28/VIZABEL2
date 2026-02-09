Unit u_frmMain;

Interface

Uses
  Winapi.Windows,
  Winapi.Messages,
  System.SysUtils,
  System.Variants,
  System.Classes,
  Vcl.Graphics,
  Vcl.Controls,
  Vcl.Forms,
  Vcl.Dialogs,
  Vcl.Buttons,
  Vcl.ExtCtrls,

  uInjectDecryptFile,
  UTInject.Constant,
  UTInject.Classes,
  UBotGestor,
  UBotConversa,
  UBotMenu,
  UTInject, Data.DB, Datasnap.DBClient, FireDAC.Stan.Intf, FireDAC.Stan.Option,
  FireDAC.Stan.Error, FireDAC.UI.Intf, FireDAC.Phys.Intf, FireDAC.Stan.Def,
  FireDAC.Stan.Pool, FireDAC.Stan.Async, FireDAC.Phys, FireDAC.Phys.MSSQL,
  FireDAC.Phys.MSSQLDef, FireDAC.VCLUI.Wait, FireDAC.Stan.Param, FireDAC.DatS,
  FireDAC.DApt.Intf, FireDAC.DApt, FireDAC.Comp.DataSet, FireDAC.Comp.Client,
  ACBrBase, ACBrValidador, REST.Types, REST.Client, Data.Bind.Components,
  Data.Bind.ObjectScope,
  JSON, Vcl.StdCtrls, ACBrUtil, System.RegularExpressions, Vcl.Grids,
  Vcl.DBGrids, System.NetEncoding, Soap.EncdDecd,
  Vcl.Imaging.jpeg, Vcl.Imaging.pngimage, System.AnsiStrings,  System.IniFiles;

type
  TRetornoMensagem = record
    IdContato: string;
    NomeContato: string;
    Mensagem: string;
    Erro: Boolean;
    Pedidofinalizado:Boolean;
  end;

Type
  TForm1 = Class(TForm)
    Panel1: TPanel;
    SpeedButton1: TSpeedButton;
    Inject1: TInject;
    rstclntApiBoot: TRESTClient;
    rstrqstApiBoot: TRESTRequest;
    rstrspnsApiBoot: TRESTResponse;
    FDConConexao: TFDConnection;
    fdqryFDQConfiguracoes: TFDQuery;
    Procedure SpeedButton1Click(Sender: TObject);
    Procedure Inject1GetUnReadMessages(Const Chats: TChatList);
    Procedure FormClose(Sender: TObject; Var Action: TCloseAction);
    procedure FormShow(Sender: TObject);
  Private
    { Private declarations }
    Gestor: TBotManager;
    ConversaAtual: TBotConversa;
    FUrlApi: String;
    procedure ProcessaOpcaoMenu(aConversa: TBotConversa; aOptionID: String);

    { *** metodos de iteração *** }
    procedure ProcessarEtapas;
    procedure ChamaAPi;
    function LerRetornoMensagemApi(const AJson: string): TRetornoMensagem;
    function RetornaURLApi:string;



    { *** fim metodos iteracao *** }

  Public
    { Public declarations }
    Procedure GestorInteracao(Conversa: TBotConversa);
    Procedure Enviar_avisoInativo();
    Procedure EnviarMensagem(AEtapa: Integer; ATexto: String;
      AAnexo: String = ''; ATipo: Integer = 0);

    property UrlApi: String read FUrlApi write FUrlApi;
  End;

Var
  Form1: TForm1;

Implementation

{$R *.dfm}


procedure TForm1.ChamaAPi;

  function EnviarMensagemWhatsApp(const AUrl, AIdContato, ANomeContato, AMensagem: string): string;
  var
    JsonBody: TJSONObject;
  begin
    Result := '';

    JsonBody := TJSONObject.Create;
    try
      JsonBody.AddPair('idContato', AIdContato);
      JsonBody.AddPair('nomecontato', ANomeContato);
      JsonBody.AddPair('mensagem', AMensagem);

      rstclntApiBoot.BaseURL := AUrl ;
      rstrqstApiBoot.Method := rmPOST;
      rstrqstApiBoot.Resource := ''; // se tiver rota, ex: '/mensagem'
      rstrqstApiBoot.Params.Clear;

      rstrqstApiBoot.AddBody(
        JsonBody.ToString,
        ctAPPLICATION_JSON
      );

      rstrqstApiBoot.Execute;

      Result := rstrspnsApiBoot.Content;
    finally
      JsonBody.Free;
    end;
  end;

var
  JsonResposta: string;
  Retorno: TRetornoMensagem;
  endpoint:String;

begin
  endpoint := '/enviarmensagem';


  JsonResposta := EnviarMensagemWhatsApp(UrlApi+endpoint,ConversaAtual.ID,ConversaAtual.Nome,ConversaAtual.Resposta);
  Retorno      := LerRetornoMensagemApi(JsonResposta);

  if (Retorno.Erro) then
  begin
    EnviarMensagem(0, 'Atendimento Finalizado.');
    ConversaAtual.Situacao := saFinalizada;
  end
  else
  begin
    ConversaAtual.Pergunta := Retorno.Mensagem;
    EnviarMensagem(1, ConversaAtual.Pergunta);

    if Retorno.Pedidofinalizado  then
    begin
     // EnviarMensagem(0, 'Atendimento Finalizado.');
      ConversaAtual.Situacao := saFinalizada;
    end;
  end;

end;

Procedure TForm1.EnviarMensagem(AEtapa: Integer; ATexto, AAnexo: String;
  ATipo: Integer);
Begin
  ConversaAtual.etapa := AEtapa;
  ConversaAtual.Pergunta := ATexto;
  ConversaAtual.Resposta := '';
  If AAnexo <> '' Then
  Begin
    If ATipo = 0 Then
    Begin
      Inject1.SendBase64('data:image/jpg;base64,' + AAnexo, ConversaAtual.ID,
        'file', ConversaAtual.Pergunta);
    End
    Else
    Begin
      Inject1.SendFile(ConversaAtual.ID, AAnexo, ConversaAtual.Pergunta);
    End;
  End
  Else If ATexto <> '' Then
  Begin
    Inject1.Send(ConversaAtual.ID, ConversaAtual.Pergunta);
  End;
End;

Procedure TForm1.Enviar_avisoInativo;
Var
  AText: String;
Begin
  AText := 'Seu tempo de atendimento foi encerrado. Para uma nova interação, digite um OI';
  EnviarMensagem(0, AText);
  ConversaAtual.Resposta := '0';
  ChamaAPi;

End;

Procedure TForm1.FormClose(Sender: TObject; Var Action: TCloseAction);
Begin
  Inject1.ShutDown();
End;

procedure TForm1.FormShow(Sender: TObject);
begin
  TChatMenu.Separator := ' ';

  UrlApi := RetornaURLApi;

end;

Procedure TForm1.ProcessaOpcaoMenu(aConversa: TBotConversa; aOptionID: String);
Begin
  (aConversa.MenuObject As TChatMenu).SendMessage('Processando Seu pedido ID:' +
    aOptionID);

  TTHread.CreateAnonymousThread(
    Procedure
    Begin
      Sleep(3000);

      (aConversa.MenuObject As TChatMenu).EncerraOpcao('Terminou de Processar');
    End).Start;
End;

procedure TForm1.ProcessarEtapas;
begin

  if (ConversaAtual.Resposta = '0') then
  begin
    ChamaAPi;
    ConversaAtual.Situacao := saFinalizada;
  end
  else
    ChamaAPi;

end;

function TForm1.RetornaURLApi: string;
begin
  FDConConexao.Connected := true;
  fdqryFDQConfiguracoes.Close;
  fdqryFDQConfiguracoes.Params.ParamByName('CHAVE').Value  := 'URL_API_BOOT';
  fdqryFDQConfiguracoes.Open();

  IF fdqryFDQConfiguracoes.RecordCount > 0 then
    Result := fdqryFDQConfiguracoes.FieldByName('VALOR').AsString;

  FDConConexao.Connected := false;
end;

Procedure TForm1.GestorInteracao(Conversa: TBotConversa);
Begin

  ConversaAtual := Conversa;

  Case Conversa.Situacao Of
    //SaNova: EtapaInicial;
    //SaEmAtendimento: ProcessarEtapas;
    SaNova: ProcessarEtapas;
    SaEmAtendimento: ProcessarEtapas;

    SaInativa:
      Enviar_avisoInativo();
    saFinalizada:
      begin
        ConversaAtual.Pergunta :=
          'Atendimento Finalizado. Para uma nova interação, digite um OI';
        EnviarMensagem(0, ConversaAtual.Pergunta);

      end;
  End;

End;

Procedure TForm1.Inject1GetUnReadMessages(Const Chats: TChatList);
Begin
  Gestor.AdministrarChatList(Inject1, Chats);
End;


function TForm1.LerRetornoMensagemApi(const AJson: string): TRetornoMensagem;
var
  JsonObj: TJSONObject;
begin
  Result := Default(TRetornoMensagem);

  JsonObj := TJSONObject.ParseJSONValue(AJson) as TJSONObject;
  try
    if JsonObj = nil then
      Exit;

    Result.IdContato   := JsonObj.GetValue<string>('idcontato');
    Result.NomeContato := JsonObj.GetValue<string>('nomecontato');
    Result.Mensagem    := JsonObj.GetValue<string>('mensagem');
    Result.Erro        := JsonObj.GetValue<Boolean>('erro');
    Result.Pedidofinalizado := JsonObj.GetValue<Boolean>('pedidofinalizado');
  finally
    JsonObj.Free;
  end;

end;



Procedure TForm1.SpeedButton1Click(Sender: TObject);
Begin
  Gestor := TBotManager.Create(Self);
  Gestor.OnInteracao := GestorInteracao;
  Gestor.Simultaneos := 20;
  Gestor.TempoInatividade := (300 * 1000); // 5 min

  If Not Inject1.Auth(false) Then
  Begin
    Inject1.FormQrCodeType := TFormQrCodeType(Ft_http);
    Inject1.FormQrCodeStart;
  End;

  If Not Inject1.FormQrCodeShowing Then
    Inject1.FormQrCodeShowing := True;
End;

End.
