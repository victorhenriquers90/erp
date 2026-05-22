# Deploy do site institucional

O app continua publicado pelo `firebase.json` principal usando a pasta `app`.

O site institucional deve ser publicado separado, usando:

```powershell
C:\laviapp\node_modules\.bin\firebase.cmd hosting:sites:create laviapp-site-46912 --project gastos-familiar-46912
C:\laviapp\node_modules\.bin\firebase.cmd deploy --config firebase.site.json --only hosting --project gastos-familiar-46912 --non-interactive
```

Depois, vincule o domínio `www.laviapp.com.br` ao Hosting site `laviapp-site-46912` no console do Firebase.

Enquanto isso não for feito, `www.laviapp.com.br` continua apontando para o Hosting atual do app.
