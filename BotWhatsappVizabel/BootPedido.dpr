program BootPedido;

uses
  Vcl.Forms,
  uTInject.ConfigCEF,
  u_frmMain in 'u_frmMain.pas' {Form1},
  uBotConversa in 'uBotConversa.pas',
  uBotGestor in 'uBotGestor.pas',
  uBotMenu in 'uBotMenu.pas';

{$R *.res}

begin
  if not GlobalCEFApp.StartMainProcess then
    exit;

  Application.Initialize;
  Application.MainFormOnTaskbar := True;
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
