unit uBotMenu;

interface

uses
  System.SysUtils,
  System.Types,
  System.UITypes,
  System.Classes,
  System.Variants,
  System.Generics.Collections,
  System.Json,

  uTInject.Constant,
  uTInject.Classes,
  uBotGestor,
  uBotConversa,
  uTInject;

type

   TChatChosenOption = Procedure (aConversa: TBotConversa; aOptionID : String) Of Object;

   TChatMenu = Class
      Private
         FJsonMenu   : TJsonObject;
         FAtual      : TJsonObject;
         FArray      : TJsonArray;
         FNiveis     : TStack<TJsonObject>;
         FOptions    : TStringList;
         FOwner      : TBotConversa;
         FInject     : TInject;
         FId         : String;
         FOnChosenOption : TChatChosenOption;
      Public
         Class Var Separator : String;
         Class Constructor CreateClass;
         Constructor Create(aOwner : TBotConversa; aInject : TInject; aJson : String);
         Destructor Destroy; Override;
         Procedure LoadJsonMenu(aJson : String);
         Procedure SendMessage(aMessage : String);
         procedure VoltaNivel;
         procedure EncerraOpcao(aMessage: String);
         Procedure ShowMenu;
         Procedure ProcessaResposta;
         Property OnChosenOption : TChatChosenOption Read FOnChosenOption  Write FOnChosenOption;
      End;

implementation


{ TChatMenu }

class constructor TChatMenu.CreateClass;
begin
Separator := ' - ';
end;

procedure TChatMenu.LoadJsonMenu(aJson: String);
Var
   Str : TStringStream;
begin
Str := TStringStream.Create;
if aJson.IsEmpty then
   Str.LoadFromFile('menu.json')
Else
   if aJson.StartsWith('file:') then
      Str.LoadFromFile(aJson.Remove(0, 5))
   Else
      Str.WriteString(aJson);
FJsonMenu := TJsonObject.ParseJSONValue(Str.DataString) As TJsonObject;
FAtual    := FJsonMenu;
If Not FAtual.TryGetValue('options', FArray) Then
   Begin
   FArray := Nil;
   raise Exception.Create('Defina um json para o menu');
   End;
Str.DisposeOf;
FNiveis.Clear;
FNiveis.Push(FJsonMenu);
ShowMenu;
end;

constructor TChatMenu.Create(aOwner: TBotConversa; aInject : TInject; aJson: String);
begin
inherited Create;
FOwner            := aOwner;
aOwner.MenuObject := Self;
FInject           := aInject;
FOptions          := TStringList.Create;
FNiveis           := TStack<TJsonObject>.Create;
FID               := '';
LoadJsonMenu(aJson);
end;

destructor TChatMenu.Destroy;
begin
FJsonMenu.DisposeOf;
FOptions .DisposeOf;
FNiveis  .DisposeOf;
inherited;
end;

procedure TChatMenu.SendMessage(aMessage: String);
begin
TThread.CreateAnonymousThread(
   Procedure
   Begin
   FInject.Send(FOwner.ID, aMessage);
   End).Start;
Sleep(1000);
end;

procedure TChatMenu.VoltaNivel;
Var
   LNivel : TJsonObject;
Begin
LNivel := FNiveis.Pop;
if LNivel = FJSonMenu then FNiveis.Push(FJsonMenu);
ShowMenu;
End;

procedure TChatMenu.EncerraOpcao(aMessage: String);
begin
if Not aMessage.IsEmpty then SendMessage(aMessage);
ShowMenu;
end;

procedure TChatMenu.ProcessaResposta;
Var
   I      : Integer;
   LArray : TJsonArray;
   LOpcao : String;
begin
LOpcao := FOwner.Resposta;
I      := FOptions.IndexOf(LOpcao);
if I = -1 then
   Begin
   SendMessage('Opção inválida.'+#13#10);
   ShowMenu;
   End
Else
   Begin
   FId := (FArray.Items[i] As TJsonObject).GetValue('id').Value;
   if Fid = '-' then
      VoltaNivel
   Else
      Begin

      FAtual := FArray.Items[I] As TJsonObject;
      FNiveis.Push(FAtual);
      ShowMenu;
      if Not FAtual.TryGetValue('options', LArray) then
         Begin
         FNiveis.Pop;
         if Assigned(FOnChosenOption) then OnChosenOption(FOwner, FId);
         End;
      End;
   End;
end;

procedure TChatMenu.ShowMenu;
Var
   MenuTexto : String;
   Opcao     : String;
   TopMenu   : TJsonObject;
   LOption   : TJsonObject;
   i         : Integer;
begin
FOptions.Clear;
TopMenu   := FNiveis.Pop;
MenuTexto := TopMenu.GetValue('text').Value+#13#10;
FArray    := nil;
If TopMenu.TryGetValue('options', FArray) Then
   Begin
   for i := 0 to FArray.Count-1 do
      Begin
      LOption   := FArray.Items[i] As TJsonObject;
      Opcao     := LOption.GetValue('option').Value+Separator+LOption.GetValue('text').Value;
      MenuTexto := MenuTexto + Opcao + #13#10;
      FOptions.Add(LOption.GetValue('option').Value);
      End;
   End;
FNiveis.Push(TopMenu);
SendMessage(MenuTexto);
end;

end.
