Document Type: IPF
item: Global
  Version=6.0
  Title English=WSP Event System Installation
  Flags=00000100
  Languages=0 0 65 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0
  LanguagesList=English
  Default Language=2
  Copy Default=1
  Japanese Font Name=MS Gothic
  Japanese Font Size=9
  Start Gradient=0 0 255
  End Gradient=0 0 0
  Windows Flags=00010100000000010010110000011000
  Log Pathname=%MAINDIR%\INSTALL.LOG
  Message Font=MS Sans Serif
  Font Size=8
  Disk Filename=SETUP
  Patch Flags=0000000000000001
  Patch Threshold=85
  Patch Memory=4000
  EXE Filename=%_BINARIES_%\Setup.exe
  FTP Cluster Size=20
  Variable Name1=_SYS_
  Variable Default1=C:\WINDOWS\system32
  Variable Flags1=00001000
  Variable Name2=_SMSINSTL_
  Variable Default2=C:\Program Files\Microsoft SMS Installer
  Variable Flags2=00001000
  Variable Name3=_BINARIES_
  Variable Description3=Specify the path for the build directory. 
  Variable Default3=\\msspades\drops\mpt\EventPubSubSystem\Latest\x86
  Variable Values3=\\msspades\drops\mpt\EventPubSubSystem\Latest\x86
  Variable Values3=\\msspades\drops\mpt\EventPubSubSystem\Latest\amd64
end
item: Open/Close INSTALL.LOG
  Flags=00000001
end
item: Check if File/Dir Exists
  Pathname=%SYS%
  Flags=10000100
end
item: Set Variable
  Variable=SYS
  Value=%WIN%
end
item: End Block
end
item: Set Variable
  Variable=APPTITLE
  Value=WSP Event System
  Flags=10000000
end
item: Set Variable
  Variable=GROUP
  Flags=10000000
end
item: Set Variable
  Variable=DISABLED
  Value=!
end
item: Parse String
  Source=%WIN%
  Pattern=:
  Variable1=SYSDRIVE
end
item: Check Configuration
  Flags=10111011
end
item: Get Registry Key Value
  Variable=COMMON
  Key=SOFTWARE\Microsoft\Windows\CurrentVersion
  Default=%SYSDRIVE%:\Program Files\Common Files
  Value Name=CommonFilesDir
  Flags=00000100
end
item: Get Registry Key Value
  Variable=PROGRAM_FILES
  Key=SOFTWARE\Microsoft\Windows\CurrentVersion
  Default=%SYSDRIVE%:\Program Files
  Value Name=ProgramFilesDir
  Flags=00000100
end
item: Set Variable
  Variable=MAINDIR
  Value=%PROGRAM_FILES%\Microsoft\WspEventSystem
  Flags=10001100
end
item: Set Variable
  Variable=EXPLORER
  Value=1
end
item: Else Statement
end
item: Set Variable
  Variable=MAINDIR
  Value=%SYSDRIVE%:\Microsoft\WspEventSystem
  Flags=10001100
end
item: End Block
end
item: Set Variable
  Variable=BACKUP
  Value=%MAINDIR%\BACKUP
  Flags=10000000
end
item: Set Variable
  Variable=DOBACKUP
  Value=B
  Flags=10000000
end
item: Set Variable
  Variable=COMPONENTS
  Flags=10000000
end
item: Set Variable
  Variable=BRANDING
  Value=0
end
item: If/While Statement
  Variable=BRANDING
  Value=1
end
item: Read INI Value
  Variable=NAME
  Pathname=%INST%\CUSTDATA.INI
  Section=REGISTRATION
  Item=NAME
end
item: Read INI Value
  Variable=COMPANY
  Pathname=%INST%\CUSTDATA.INI
  Section=REGISTRATION
  Item=COMPANY
end
item: If/While Statement
  Variable=NAME
end
item: Set Variable
  Variable=DOBRAND
  Value=1
end
item: End Block
end
item: End Block
end
item: Set Variable
  Variable=WSPQUEUESIZE
  Value=102400000
  Flags=10000000
end
item: Set Variable
  Variable=WSPAVEEVNTSIZE
  Value=10240
  Flags=10000000
end
item: Set Variable
  Variable=WSPRPORT
  Value=1300
  Flags=10000000
end
item: Set Variable
  Variable=WSPRBUFFERSIZE
  Value=1024000
  Flags=10000000
end
item: Set Variable
  Variable=WSPRTIMEOUT
  Value=20000
  Flags=10000000
end
item: Set Variable
  Variable=WSPPARENTNAME
  Value= 
  Flags=10000000
end
item: Set Variable
  Variable=WSPNIC
  Value= 
  Flags=10000000
end
item: Set Variable
  Variable=WSPPPORT
  Value=1300
  Flags=10000000
end
item: Set Variable
  Variable=WSPPBUFFERSIZE
  Value=1024000
  Flags=10000000
end
item: Set Variable
  Variable=WSPPTIMEOUT
  Value=20000
  Flags=10000000
end
item: Set Variable
  Variable=WSPOUTQSIZE
  Value=102400000
  Flags=10000000
end
item: Set Variable
  Variable=WSPOUTQTIMEOUT
  Value=600
  Flags=10000000
end
item: Wizard Block
  Direction Variable=DIRECTION
  Display Variable=DISPLAY
  Bitmap Pathname=%_SMSINSTL_%\DIALOGS\TEMPLATE\WIZARD.BMP
  X Position=9
  Y Position=10
  Filler Color=8421440
  Dialog=Select Program Manager Group
  Dialog=Select Backup Directory
  Dialog=Display Registration Information
  Dialog=Get Registration Information
  Variable=EXPLORER
  Variable=DOBACKUP
  Variable=DOBRAND
  Variable=DOBRAND
  Value=1
  Value=A
  Value=1
  Value=1
  Compare=0
  Compare=1
  Compare=0
  Compare=1
  Flags=00000011
end
item: Custom Dialog Set
  Name=Welcome
  Display Variable=DISPLAY
  item: Dialog
    Title Danish=%APPTITLE% Installation
    Title Dutch=Installatie van %APPTITLE%
    Title English=%APPTITLE% Installation
    Title Finnish=Asennus: %APPTITLE%
    Title French=Installation de %APPTITLE%
    Title German=Installation von %APPTITLE%
    Title Italian=Installazione di %APPTITLE%
    Title Norwegian=Installere %APPTITLE%
    Title Portuguese=Instalação de %APPTITLE%
    Title Spanish=Instalación de %APPTITLE%
    Title Swedish=Installation av %APPTITLE%
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Static
      Rectangle=86 8 258 42
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Velkommen!
      Text Dutch=Welkom!
      Text English=Welcome!
      Text Finnish=Tervetuloa
      Text French=Bienvenue !
      Text German=Willkommen!
      Text Italian=Benvenuti
      Text Norwegian=Velkommen!
      Text Portuguese=Bem-vindo!
      Text Spanish=Bienvenido
      Text Swedish=Välkommen!
    end
    item: Push Button
      Rectangle=151 231 196 246
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=106 231 151 246
      Variable=DISABLED
      Value=!
      Create Flags=01010000000000010000000000000000
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=212 231 257 246
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=86 42 256 131
      Create Flags=01010000000000000000000000000000
      Text Danish=Dette installationsprogram vil installere %APPTITLE%.
      Text Danish=
      Text Danish=Tryk på Næste for at starte installationen. Du kan trykke på Annuller, hvis du ikke vil installere %APPTITLE% nu.
      Text Dutch=Met dit installatieprogramma wordt %APPTITLE% geïnstalleerd.
      Text Dutch=
      Text Dutch=Kies de knop Volgende om de installatie te starten. Kies Annuleren als u %APPTITLE% nu niet wilt installeren.
      Text English=This installation program will install %APPTITLE%.
      Text English=
      Text English=Press the Next button to start the installation. You can press the Cancel button now if you do not want to install %APPTITLE% at this time.
      Text Finnish=Tämä asennusohjelma asentaa kohteen %APPTITLE%.
      Text Finnish=
      Text Finnish=Aloita asennus valitsemalla Seuraava. Voit valita Peruuta, jos et halua asentaa kohdetta  %APPTITLE% nyt.
      Text French=Ce programme d'installation va installer %APPTITLE%.
      Text French=
      Text French=Cliquez sur le bouton Suivant pour démarrer l'installation. Vous pouvez cliquer sur le bouton Annuler maintenant si vous ne voulez pas installer %APPTITLE% dès à présent.
      Text German=Mit diesem Installationsprogramm wird %APPTITLE% installiert.
      Text German=
      Text German=Klicken Sie auf "Weiter", um mit der Installation zu beginnen. Klicken Sie auf "Abbrechen", um die Installation von %APPTITLE% abzubrechen.
      Text Italian=Verrà installato %APPTITLE%.
      Text Italian=
      Text Italian=Per avviare l'installazione fare clic sul pulsante Avanti. Se non si desidera installare %APPTITLE% ora, scegliere Annulla.
      Text Norwegian=Dette installasjonsprogrammet installerer %APPTITLE%.
      Text Norwegian=
      Text Norwegian=Velg Neste for å starte installasjonen. Velg Avbryt hvis du ikke vil installere %APPTITLE% nå.
      Text Portuguese=Este programa de instalação instalará %APPTITLE%.
      Text Portuguese=
      Text Portuguese=Prima o botão 'Seguinte' para iniciar a instalação. Pode premir o botão 'Cancelar' agora se não desejar instalar %APPTITLE% neste momento.
      Text Spanish=Este programa de instalación instalará %APPTITLE%.
      Text Spanish=
      Text Spanish=Elija Siguiente para iniciar la instalación. Elija Cancelar si no desea instalar %APPTITLE% en este momento.
      Text Swedish=Det här installationsprogrammet kommer att installera %APPTITLE%.
      Text Swedish=
      Text Swedish=Klicka på Nästa för att påbörja installationen eller klicka på Bakåt för att skriva om installationsinformationen.
    end
    item: Static
      Rectangle=9 224 257 225
      Action=3
      Create Flags=01010000000000000000000000000111
    end
  end
end
item: Custom Dialog Set
  Name=Select Destination Directory
  Display Variable=DISPLAY
  item: Dialog
    Title Danish=%APPTITLE% Installation
    Title Dutch=Installatie van %APPTITLE%
    Title English=%APPTITLE% Installation
    Title Finnish=Asennus: %APPTITLE%
    Title French=Installation de %APPTITLE%
    Title German=Installation von %APPTITLE%
    Title Italian=Installazione di %APPTITLE%
    Title Norwegian=Installere %APPTITLE%
    Title Portuguese=Instalação de %APPTITLE%
    Title Spanish=Instalación de %APPTITLE%
    Title Swedish=Installation av %APPTITLE%
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=150 233 195 248
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=105 233 150 248
      Variable=DIRECTION
      Value=B
      Create Flags=01010000000000010000000000000000
      Flags=0000000000000001
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=211 233 256 248
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=8 226 256 227
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=86 8 258 42
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Vælg destinationsmappe
      Text Dutch=Doelmap selecteren
      Text English=Select Destination Directory
      Text Finnish=Valitse kohdekansio
      Text French=Sélectionner un répertoire de destination 
      Text German=Zielverzeichnis wählen
      Text Italian=Selezionare la directory di destinazione
      Text Norwegian=Velg målkatalog
      Text Portuguese=Seleccione o directório de destino
      Text Spanish=Seleccione el directorio de destino
      Text Swedish=Markera målkatalog
    end
    item: Static
      Rectangle=85 41 255 113
      Create Flags=01010000000000000000000000000000
      Text Danish=Vælg den mappe, som %APPTITLE%-filerne skal installeres i.
      Text Danish=
      Text Danish="Ledig diskplads efter installation'" er baseret på det aktuelle valg af filer, der skal installeres. Et negativt tal betyder, at der ikke er nok ledig diskplads til at installere programmet på det angivne drev.
      Text Dutch=Selecteer de map waarin de bestanden voor %APPTITLE% geïnstalleerd dienen te worden.
      Text Dutch=
      Text Dutch=Het getal voor "Beschikbare schijfruimte na installatie" is gebaseerd op de bestanden die u momenteel voor installatie hebt geselecteerd. Een negatief getal betekent dat er onvoldoende schijfruimte aanwezig is om de toepassing te installeren op het opgegeven station.
      Text English=Please select the directory where %APPTITLE% files are to be installed. 
      Text English=
      Text English="Free Disk Space After Install" is based on your current selection of files to install.  A negative number indicates that there is not enough disk space to install the application to the specified drive.
      Text Finnish=Valitse kansio, johon %APPTITLE% -tiedostot asennetaan.
      Text Finnish=
      Text Finnish="Vapaa levytila asennuksen jälkeen" perustuu nykyiseen asennettavien tiedostojen valintaan. Negatiivinen arvo osoittaa, ettei valitussa asemassa ole tarpeeksi tilaa valitun ohjelman asentamiseen.
      Text French=Sélectionnez le répertoire dans lequel les fichiers de %APPTITLE% doivent être installés.
      Text French=
      Text French=L'espace disque disponible après l'installation dépend de la sélection actuelle de fichiers à installer. Un nombre négatif indique que l'espace disque n'est pas suffisant pour installer l'application sur le lecteur spécifié.
      Text German=Geben Sie an, in welchem Verzeichnis die %APPTITLE%-Dateien installiert werden sollen.
      Text German=
      Text German="Freier Speicherplatz nach der Installation" bezieht sich auf die aktuelle Auswahl an Dateien. Eine negative Zahl gibt an, dass es nicht genug Speicher auf dem angegebenen Laufwerk gibt, um die Anwendung dort zu installieren.
      Text Italian=Selezionare la directory in cui verranno installati i file %APPTITLE%.
      Text Italian=
      Text Italian=Il valore di "Spazio disponibile su disco dopo l'installazione" è calcolato in base ai file selezionati per l'installazione. Un valore negativo indica che lo spazio disponibile sull'unità specificata non è sufficiente per l'installazione dell'applicazione.
      Text Norwegian=Velg hvilken katalog %APPTITLE%-filene skal installeres i.
      Text Norwegian=
      Text Norwegian="Ledig diskplass etter installasjon" baseres på hvilke filer du har valgt å installere. Et negativt tall betyr at det ikke er nok diskplass til å installere programmet på angitt stasjon.
      Text Portuguese=Seleccione o directório onde serão instalados os ficheiros de %APPTITLE%.
      Text Portuguese=
      Text Portuguese="Espaço livre em disco depois da instalação'" é baseado na selecção actual de ficheiros a instalar.  Um número negativo indica que não existe espaço suficiente para instalar a aplicação na unidade indicada.
      Text Spanish=Seleccione el directorio donde desee instalar los archivos de %APPTITLE%.
      Text Spanish=
      Text Spanish="Espacio después de instalar'" tiene en cuenta la selección actual de archivos que desea instalar.  Un número negativo indica que no hay suficiente espacio en disco para instalar la aplicación en la unidad especificada.
      Text Swedish=Markera katalogen där %APPTITLE%-filerna ska installeras.
      Text Swedish=
      Text Swedish="Ledigt diskutrymme efter installation" baseras på vilka filer som är markerade för installation. Ett negativt tal innebär att det inte finns tillräckligt med diskutrymme för att installera programmet på angiven enhet.
    end
    item: Static
      Rectangle=85 110 259 137
      Action=1
      Create Flags=01010000000000000000000000000111
    end
    item: Push Button
      Rectangle=208 118 253 133
      Variable=MAINDIR_SAVE
      Value=%MAINDIR%
      Destination Dialog=1
      Action=2
      Create Flags=01010000000000010000000000000000
      Text Danish=&Gennemse...
      Text Dutch=B&laderen...
      Text English=B&rowse...
      Text Finnish=S&elaa...
      Text French=Parco&urir...
      Text German=Durchsuchen...
      Text Italian=&Sfoglia
      Text Norwegian=&Bla gjennom...
      Text Portuguese=P&rocurar...
      Text Spanish=E&xaminar...
      Text Swedish= B&läddra...
    end
    item: Static
      Rectangle=90 121 206 132
      Destination Dialog=2
      Create Flags=01010000000000000000000000000000
      Text Danish=%MAINDIR%
      Text Dutch=%MAINDIR%
      Text English=%MAINDIR%
      Text Finnish=%MAINDIR%
      Text French=%MAINDIR%
      Text German=%MAINDIR%
      Text Italian=%MAINDIR%
      Text Norwegian=%MAINDIR%
      Text Portuguese=%MAINDIR%
      Text Spanish=%MAINDIR%
      Text Swedish=%MAINDIR% 
    end
    item: Static
      Rectangle=204 158 257 168
      Variable=COMPONENTS
      Value=MAINDIR
      Create Flags=01010000000000000000000000000010
    end
    item: Static
      Rectangle=204 148 257 157
      Value=MAINDIR
      Create Flags=01010000000000000000000000000010
    end
    item: Static
      Rectangle=90 148 190 159
      Create Flags=01010000000000000000000000000000
      Text Danish=Nuværende ledig diskplads:
      Text Dutch=Beschikbare schijfruimte:
      Text English=Current Free Disk Space:
      Text Finnish=Nykyinen vapaa levytila:
      Text French=Espace disque disponible actuel :
      Text German=Freier Speicherplatz:
      Text Italian=Spazio disponibile su disco:
      Text Norwegian=Ledig diskplass:
      Text Portuguese=Espaço livre em disco:
      Text Spanish=Espacio en disco actual:
      Text Swedish=Ledigt diskutrymme:
    end
    item: Static
      Rectangle=90 158 216 168
      Create Flags=01010000000000000000000000000000
      Text Danish=Ledig diskplads efter installation:
      Text Dutch=Beschikbare schijfruimte na installatie:
      Text English=Free Disk Space After Install:
      Text Finnish=Vapaa levytila asennuksen jälkeen:
      Text French=Espace disque disponible après l'installation :
      Text German=Freier Speicherplatz nach der Installation:
      Text Italian=Spazio disponibile su disco dopo l'installazione:
      Text Norwegian=Ledig diskplass etter installasjon:
      Text Portuguese=Espaço livre em disco depois da instalação:
      Text Spanish=Espacio después de instalar:
      Text Swedish=Ledigt diskutrymme efter installation:
    end
    item: Static
      Rectangle=85 140 259 170
      Action=1
      Create Flags=01010000000000000000000000000111
    end
  end
  item: Dialog
    Title Danish=Vælg destinationsmappe
    Title Dutch=Doelmap selecteren
    Title English=Select Destination Directory
    Title Finnish=Valitse kohdekansio
    Title French=Sélectionner un répertoire de destination 
    Title German=Zielverzeichnis wählen
    Title Italian=Selezionare la directory di destinazione
    Title Norwegian=Velg målkatalog
    Title Portuguese=Seleccione o directório de destino
    Title Spanish=Seleccione el directorio de destino
    Title Swedish=Markera målkatalog
    Width=221
    Height=173
    Font Name=Helv
    Font Size=8
    item: Listbox
      Rectangle=5 5 163 149
      Variable=MAINDIR_SAVE
      Create Flags=01010000100000010000000101000000
      Flags=0000110000100010
      Text Danish=%MAINDIR%
      Text Dutch=%MAINDIR%
      Text English=%MAINDIR%
      Text Finnish=%MAINDIR%
      Text French=%MAINDIR%
      Text German=%MAINDIR%
      Text Italian=%MAINDIR%
      Text Norwegian=%MAINDIR%
      Text Portuguese=%MAINDIR%
      Text Spanish=%MAINDIR%
      Text Swedish=%MAINDIR%
    end
    item: Push Button
      Rectangle=167 6 212 21
      Variable=MAINDIR
      Value=%MAINDIR_SAVE%
      Create Flags=01010000000000010000000000000001
      Text Danish=OK
      Text Dutch=OK
      Text English=OK
      Text Finnish=OK
      Text French=OK
      Text German=OK
      Text Italian=OK
      Text Norwegian=OK
      Text Portuguese=OK
      Text Spanish=Aceptar
      Text Swedish=OK
    end
    item: Push Button
      Rectangle=167 25 212 40
      Create Flags=01010000000000010000000000000000
      Flags=0000000000000001
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
  end
end
item: Custom Dialog Set
  Name=Select Program Manager Group
  Display Variable=DISPLAY
  item: Dialog
    Title Danish=%APPTITLE% Installation
    Title Dutch=Installatie van %APPTITLE%
    Title English=%APPTITLE% Installation
    Title Finnish=Asennus: %APPTITLE%
    Title French=Installation de %APPTITLE%
    Title German=Installation von %APPTITLE%
    Title Italian=Installazione di %APPTITLE%
    Title Norwegian=Installere %APPTITLE%
    Title Portuguese=Instalação de %APPTITLE%
    Title Spanish=Instalación de %APPTITLE%
    Title Swedish=Installation av %APPTITLE%
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=151 230 196 245
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=103 230 148 245
      Variable=DIRECTION
      Value=B
      Create Flags=01010000000000010000000000000000
      Flags=0000000000000001
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=210 230 255 245
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=9 222 257 223
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=86 8 258 42
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Vælg gruppe i Programstyring
      Text Dutch=Groep in ProgMan selecteren
      Text English=Select ProgMan Group
      Text Finnish=Valitse Järjestelmänhallinnan ryhmä
      Text French=Sélectionner le groupe du Gestionnaire de programmes
      Text German=Bestimmung der Programm-Managergruppe
      Text Italian=Selezionare il gruppo di Program Manager
      Text Norwegian=Velg programbehandlingsgruppe
      Text Portuguese=Seleccione o grupo do Gestor de programas
      Text Spanish=Seleccione Grupo del Admin. prog.
      Text Swedish=Markera en grupp i Programhanteraren
    end
    item: Static
      Rectangle=86 44 256 68
      Create Flags=01010000000000000000000000000000
      Text Danish=Skriv navnet på den gruppe i Programstyring, som %APPTITLE%-ikonerne skal føjes til:
      Text Dutch=Voer de naam in van de groep in Programmabeheer waar u de pictogrammen voor %APPTITLE% aan wilt toevoegen:
      Text English=Enter the name of the Program Manager group to which to add %APPTITLE% icons:
      Text Finnish=Valitse Järjestelmänhallinnan ryhmä, johon haluat lisätä kohteen  %APPTITLE% kuvakkeet:
      Text French=Entrez le nom du groupe du Gestionnaire de programme dans lequel vous souhaitez ajouter les icônes de %APPTITLE% :
      Text German=Geben Sie den Namen der Programmgruppe ein, der das Symbol %APPTITLE% hinzugefügt werden soll:
      Text Italian=Immettere il nome del gruppo di Program Manager a cui aggiungere le icone %APPTITLE%:
      Text Norwegian=Angi navnet på programbehandlingsgruppen hvor %APPTITLE%-ikonene skal legges til:
      Text Portuguese=Escreva o nome do grupo do 'Gestor de programas' onde serão adicionados os ícones de %APPTITLE%:
      Text Spanish=Escriba el nombre del grupo del Administrador de programas al que desea agregar los iconos de %APPTITLE%:
      Text Swedish=Skriv namnet på gruppen i Programhanteraren dit du vill lägga %APPTITLE%-ikonerna:
    end
    item: Combobox
      Rectangle=86 69 256 175
      Variable=GROUP
      Create Flags=01010000001000010000001100000001
      Flags=0000000000000001
      Text Danish=%GROUP%
      Text Dutch=%GROUP%
      Text English=%GROUP%
      Text Finnish=%GROUP%
      Text French=%GROUP%
      Text German=%GROUP%
      Text Italian=%GROUP%
      Text Norwegian=%GROUP%
      Text Portuguese=%GROUP%
      Text Spanish=%GROUP%
      Text Swedish=%GROUP%
    end
  end
end
item: Custom Dialog Set
  Name=WSP Router Default Values
  Display Variable=DISPLAY
  item: Dialog
    Title English=WSP Router Settings
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=151 238 196 253
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=103 238 148 253
      Variable=DIRECTION
      Value=B
      Create Flags=01010000000000010000000000000000
      Flags=0000000000000001
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=210 238 255 253
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=7 225 255 226
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=91 13 263 47
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Vælg gruppe i Programstyring
      Text Dutch=Groep in ProgMan selecteren
      Text English=WSP Router Config Settings
      Text Finnish=Valitse Järjestelmänhallinnan ryhmä
      Text French=Sélectionner le groupe du Gestionnaire de programmes
      Text German=Bestimmung der Programm-Managergruppe
      Text Italian=Selezionare il gruppo di Program Manager
      Text Norwegian=Velg programbehandlingsgruppe
      Text Portuguese=Seleccione o grupo do Gestor de programas
      Text Spanish=Seleccione Grupo del Admin. prog.
      Text Swedish=Markera en grupp i Programhanteraren
    end
    item: Editbox
      Rectangle=169 39 241 54
      Variable=WSPQUEUESIZE
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPQUEUESIZE%
    end
    item: Static
      Rectangle=83 40 165 55
      Create Flags=01010000000000000000000000000000
      Text English=Shared Memory Size (bytes):
    end
    item: Static
      Rectangle=83 61 158 76
      Create Flags=01010000000000000000000000000000
      Text English=Average Event Size (bytes):
    end
    item: Editbox
      Rectangle=169 61 241 76
      Variable=WSPAVEEVNTSIZE
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPAVEEVNTSIZE%
    end
    item: Editbox
      Rectangle=168 110 240 125
      Variable=WSPRPORT
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPRPORT%
    end
    item: Static
      Rectangle=83 110 138 125
      Create Flags=01010000000000000000000000000000
      Text English=Listen Port Number    (0 to not listen)
    end
    item: Editbox
      Rectangle=169 132 241 147
      Variable=WSPRBUFFERSIZE
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPRBUFFERSIZE%
    end
    item: Static
      Rectangle=83 134 158 149
      Create Flags=01010000000000000000000000000000
      Text English=Buffer Size (bytes):
    end
    item: Editbox
      Rectangle=169 153 241 168
      Variable=WSPRTIMEOUT
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPRTIMEOUT%
    end
    item: Static
      Rectangle=83 152 158 167
      Create Flags=01010000000000000000000000000000
      Text English=Timeout (ms):
    end
    item: Editbox
      Rectangle=168 86 240 101
      Variable=WSPNIC
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPNIC%
    end
    item: Static
      Rectangle=83 85 158 100
      Create Flags=01010000000000000000000000000000
      Text English=Listen IP Address         (Blank for default)
    end
    item: Static
      Rectangle=83 174 161 193
      Create Flags=01010000000000000000000000000000
      Text English=Max Out Communication Queue Size (bytes)
    end
    item: Static
      Rectangle=83 196 161 215
      Create Flags=01010000000000000000000000000000
      Text English=Max Out Communication Timeout (seconds)
    end
    item: Editbox
      Rectangle=168 176 240 191
      Variable=WSPOUTQSIZE
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPOUTQSIZE%
    end
    item: Editbox
      Rectangle=169 199 241 214
      Variable=WSPOUTQTIMEOUT
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPOUTQTIMEOUT%
    end
  end
end
item: Custom Dialog Set
  Name=WSP Parent Router
  Display Variable=DISPLAY
  item: Dialog
    Title English=WSP Parent Router
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=154 231 199 246
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=106 231 151 246
      Variable=DIRECTION
      Value=B
      Create Flags=01010000000000010000000000000000
      Flags=0000000000000001
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=213 231 258 246
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=10 224 258 225
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=89 10 262 33
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Vælg gruppe i Programstyring
      Text Dutch=Groep in ProgMan selecteren
      Text English=WSP Parent Router Settings
      Text Finnish=Valitse Järjestelmänhallinnan ryhmä
      Text French=Sélectionner le groupe du Gestionnaire de programmes
      Text German=Bestimmung der Programm-Managergruppe
      Text Italian=Selezionare il gruppo di Program Manager
      Text Norwegian=Velg programbehandlingsgruppe
      Text Portuguese=Seleccione o grupo do Gestor de programas
      Text Spanish=Seleccione Grupo del Admin. prog.
      Text Swedish=Markera en grupp i Programhanteraren
    end
    item: Static
      Rectangle=84 65 159 80
      Create Flags=01010000000000000000000000000000
      Text English=Parent Machine Name:
    end
    item: Editbox
      Rectangle=168 65 240 80
      Variable=WSPPARENTNAME
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPPARENTNAME%
    end
    item: Editbox
      Rectangle=168 93 240 108
      Variable=WSPPPORT
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPPPORT%
    end
    item: Static
      Rectangle=84 93 159 108
      Create Flags=01010000000000000000000000000000
      Text English=TCP Port Number
    end
    item: Editbox
      Rectangle=168 121 240 136
      Variable=WSPPBUFFERSIZE
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPPBUFFERSIZE%
    end
    item: Static
      Rectangle=84 120 159 135
      Create Flags=01010000000000000000000000000000
      Text English=Buffer Size (bytes):
    end
    item: Editbox
      Rectangle=168 148 240 163
      Variable=WSPPTIMEOUT
      Help Context=16711681
      Create Flags=01010000100000010000000000000000
      Text English=%WSPPTIMEOUT%
    end
    item: Static
      Rectangle=84 147 159 162
      Create Flags=01010000000000000000000000000000
      Text English=Timeout (ms):
    end
    item: Static
      Rectangle=93 34 256 53
      Create Flags=01010000000000000000000000000000
      Text English=If this Router is a child of another Router then specify the information of its parent
    end
  end
end
item: Custom Dialog Set
  Name=Start Installation
  Display Variable=DISPLAY
  item: Dialog
    Title Danish=%APPTITLE% Installation
    Title Dutch=Installatie van %APPTITLE%
    Title English=%APPTITLE% Installation
    Title Finnish=Asennus: %APPTITLE%
    Title French=Installation de %APPTITLE%
    Title German=Installation von %APPTITLE%
    Title Italian=Installazione di %APPTITLE%
    Title Norwegian=Installere %APPTITLE%
    Title Portuguese=Instalação de %APPTITLE%
    Title Spanish=Instalación de %APPTITLE%
    Title Swedish=Installation av %APPTITLE%
    Width=280
    Height=280
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=150 227 195 242
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Næste >
      Text Dutch=&Volgende >
      Text English=&Next >
      Text Finnish=&Seuraava >
      Text French=&Suivant >
      Text German=&Weiter >
      Text Italian=&Avanti >
      Text Norwegian=&Neste >
      Text Portuguese=&Seguinte >
      Text Spanish=&Siguiente >
      Text Swedish= &Nästa >
    end
    item: Push Button
      Rectangle=105 227 150 242
      Variable=DIRECTION
      Value=B
      Create Flags=01010000000000010000000000000000
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=211 227 256 242
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=8 220 256 221
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=86 8 258 42
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Klar til at installere!
      Text Dutch=Klaar om te installeren!
      Text English=Ready to Install!
      Text Finnish=Valmis asentamaan.
      Text French=Prêt à installer !
      Text German=Installationsbereit!
      Text Italian=Pronto per l'installazione
      Text Norwegian=Klar til å installere.
      Text Portuguese=Pronto para instalar!
      Text Spanish=Preparado para la instalación.
      Text Swedish=Klart för installation.
    end
    item: Static
      Rectangle=86 42 256 102
      Create Flags=01010000000000000000000000000000
      Text Danish=Du er nu klar til at installere %APPTITLE%.
      Text Danish=
      Text Danish=Tryk på Næste for at starte installationen, eller tryk på Tilbage for at ændre installationsoplysningerne.
      Text Dutch=%APPTITLE% kan nu geïnstalleerd worden.
      Text Dutch=
      Text Dutch=Kies de knop Volgende als u de installatie wilt starten, of kies Terug als u de installatiegegevens opnieuw wilt invoeren.
      Text English=You are now ready to install %APPTITLE%.
      Text English=
      Text English=Press the Next button to begin the installation or the Back button to re-enter the installation information.
      Text Finnish=Kohteen %APPTITLE% voi nyt asentaa.
      Text Finnish=
      Text Finnish=Aloita asennus valitsemalla Seuraava tai määritä asennustiedot uudelleen valitsemalla Edellinen.
      Text French=Vous êtes maintenant prêt à installer %APPTITLE%.
      Text French=
      Text French=Cliquez sur le bouton Suivant pour commencer l'installation ou sur le bouton Précédent pour entrer de nouveau les informations d'installation.
      Text German=Sie können %APPTITLE% nun installieren.
      Text German=
      Text German=Klicken Sie auf "Weiter", um mit der Installation zu beginnen. Klicken Sie auf "Zurück", um die Installationsinformationen neu einzugeben.
      Text Italian=Ora è possibile installare %APPTITLE%.
      Text Italian=
      Text Italian=Fare clic sul pulsante Avanti per avviare l'installazione o sul pulsante Indietro per reimmettere le informazioni sull'installazione.
      Text Norwegian=%APPTITLE% kan nå installeres.
      Text Norwegian=
      Text Norwegian=Velg Neste for å starte installasjonen, eller velgTilbake for å angi installasjonsinformasjonen på nytt.
      Text Portuguese=Está pronto para instalar %APPTITLE%.
      Text Portuguese=
      Text Portuguese=Prima o botão 'Seguinte' para iniciar a instalação ou no botão 'Anterior' para voltar a introduzir as informações de instalação.
      Text Spanish=Ya está listo para instalar %APPTITLE%.
      Text Spanish=
      Text Spanish=Elija Siguiente para iniciar la instalación o Atrás para introducir de nuevo la información para la instalación.
      Text Swedish=Du kan nu installera %APPTITLE%.
      Text Swedish=
      Text Swedish=Välj Nästa för att påbörja installationen eller klicka på Föregående om du vill skriva om installationsinformationen.
    end
  end
end
item: If/While Statement
  Variable=DISPLAY
  Value=Select Destination Directory
end
item: Set Variable
  Variable=BACKUP
  Value=%MAINDIR%\BACKUP
end
item: End Block
end
item: End Block
end
item: If/While Statement
  Variable=DOBACKUP
  Value=A
end
item: Set Variable
  Variable=BACKUPDIR
  Value=%BACKUP%
end
item: End Block
end
item: If/While Statement
  Variable=BRANDING
  Value=1
end
item: If/While Statement
  Variable=DOBRAND
  Value=1
end
item: Edit INI File
  Pathname=%INST%\CUSTDATA.INI
  Settings=[REGISTRATION]
  Settings=NAME=%NAME%
  Settings=COMPANY=%COMPANY%
  Settings=
end
item: End Block
end
item: End Block
end
item: Open/Close INSTALL.LOG
end
item: Check Disk Space
  Component=COMPONENTS
end
item: Include Script
  Pathname=%_SMSINSTL_%\Include\uninstal.ipf
end
item: Add Text to INSTALL.LOG
  Text=Execute path: %MAINDIR%\PerformanceCounterSetup /d
end
item: Add Directory to Path
  Directory=%MAINDIR%
end
item: Install File
  Source=%_BINARIES_%\PerformanceCounterSetup.exe
  Destination=%MAINDIR%\PerformanceCounterSetup.exe
  Flags=0000000000000010
end
item: Execute Program
  Pathname=%MAINDIR%\PerformanceCounterSetup
  Flags=00001010
end
item: Install File
  Source=%_BINARIES_%\WspSharedQueue.dll
  Destination=%MAINDIR%\Client Application Binaries\WspSharedQueue.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspEvent.dll
  Destination=%MAINDIR%\Client Application Binaries\WspEvent.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\SharedMemoryMgr.dll
  Destination=%MAINDIR%\Client Application Binaries\SharedMemoryMgr.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\PubSubMgr.dll
  Destination=%MAINDIR%\Client Application Binaries\PubSubMgr.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\PubSubMgr.dll
  Destination=%MAINDIR%\PubSubMgr.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\PubSubMgr.pdb
  Destination=%MAINDIR%\PubSubMgr.pdb
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\SharedMemoryMgr.dll
  Destination=%MAINDIR%\SharedMemoryMgr.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\SharedMemoryMgr.pdb
  Destination=%MAINDIR%\SharedMemoryMgr.pdb
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspEvent.dll
  Destination=%MAINDIR%\WspEvent.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspEvent.pdb
  Destination=%MAINDIR%\WspEvent.pdb
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspEventRouter.exe
  Destination=%MAINDIR%\WspEventRouter.exe
  Flags=0000000000000010
end
item: Add Text to INSTALL.LOG
  Text=File Copy: %MAINDIR%\WspEventRouter.exe.config
end
item: Install File
  Source=%_BINARIES_%\WspEventRouter.pdb
  Destination=%MAINDIR%\WspEventRouter.pdb
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspSharedQueue.dll
  Destination=%MAINDIR%\WspSharedQueue.dll
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspSharedQueue.pdb
  Destination=%MAINDIR%\WspSharedQueue.pdb
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\WspEventSystem.chm
  Destination=%MAINDIR%\WspEventSystem.chm
  Flags=0000000000000010
end
item: Install File
  Source=%_BINARIES_%\Samples
  Destination=%MAINDIR%\Samples
  Flags=0000000100000010
end
item: Find File in Path
  Variable=GACUTIL
  Pathname List=gacutil.exe
end
item: Delete File
  Pathname=%TEMP%\gacfiles.cmd
end
item: Insert Line into Text File
  Pathname=%TEMP%\gacfiles.cmd
  New Text="%GACUTIL%" /i "%MAINDIR%\WspEvent.dll"
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%TEMP%\gacfiles.cmd
  New Text="%GACUTIL%" /i "%MAINDIR%\PubSubMgr.dll"
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%TEMP%\gacfiles.cmd
  New Text="%GACUTIL%" /i "%MAINDIR%\WspSharedQueue.dll"
  Line Number=0
end
item: Add Text to INSTALL.LOG
  Text=Execute path: "%GACUTIL%" /u WspEvent
end
item: Add Text to INSTALL.LOG
  Text=Execute path: "%GACUTIL%" /u PubSubMgr
end
item: Add Text to INSTALL.LOG
  Text=Execute path: "%GACUTIL%" /u WspSharedQueue
end
item: Delete File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=<?xml version="1.0" encoding="utf-8" ?>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=<configuration>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  <configSections>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <section name="eventRouterSettings" type="foo"/>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <section name="eventPersistSettings" type="foo2"/>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  </configSections>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  <eventRouterSettings>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- refreshIncrement should be about 1/3 of what the expirationIncrement is.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- This setting needs to be consistent across all the machines in the eventing network.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <subscriptionManagement refreshIncrement="3"  expirationIncrement="10"/>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <localPublish eventQueueName="WspEventQueue" eventQueueSize="%WSPQUEUESIZE%" averageEventSize="%WSPAVEEVNTSIZE%"/>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- These settings control what should happen to an output queue when communications is lost to a parent or child.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- maxQueueSize is in bytes and maxTimeout is in seconds.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- When the maxQueueSize is reached or the maxTimeout is reached for a communication that has been lost, the queue is deleted.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <outputCommunicationQueues maxQueueSize="%WSPOUTQSIZE%" maxTimeout="%WSPOUTQTIMEOUT%"/>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- nic can be an alias which specifies a specific IP address or an IP address. -->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- port can be 0 if you don't want to have the router open a listening port to be a parent to other routers. -->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <thisRouter nic="%WSPNIC%" port="%WSPRPORT%" bufferSize="%WSPRBUFFERSIZE%" timeout="%WSPRTIMEOUT%"/>
  Line Number=0
end
item: If/While Statement
  Variable=WSPPARENTNAME
  Value= 
  Flags=00000001
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <parentRouter name="%WSPPARENTNAME%" port="%WSPPPORT%" bufferSize="%WSPPBUFFERSIZE%" timeout="%WSPPTIMEOUT%"/>
  Line Number=0
end
item: Else Statement
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- <parentRouter name="ParentMachineName" port="%WSPPPORT%" bufferSize="%WSPPBUFFERSIZE%" timeout="%WSPPTIMEOUT%"/>  -->
  Line Number=0
end
item: End Block
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  </eventRouterSettings>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  <eventPersistSettings>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- type specifies the EventType to be persisted.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- localOnly is a boolean which specifies whether only events published on this machine are persisted or if events from the entire network are persisted.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- maxFileSize specifies the maximum size in bytes that the persisted file should be before it is copied.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- maxCopyInterval specifies in seconds the longest time interval before the persisted file is copied.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- fieldTerminator specifies the character used between fields.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- rowTerminator specifies the character used at the end of each row written.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- tempFileDirectory is the local directory used for writing out the persisted event data.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- copyToFileDirectory is the final destination of the persisted data file. It can be local or remote using a UNC.-->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=    <!-- <event type="78422526-7B21-4559-8B9A-BC551B46AE34" localOnly="true" maxFileSize="2000000000" maxCopyInterval="60" fieldTerminator="," rowTerminator="\n" tempFileDirectory="c:\temp\WebEvents\" copyToFileDirectory="c:\temp\WebEvents\log\" /> -->
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=  </eventPersistSettings>
  Line Number=0
end
item: Insert Line into Text File
  Pathname=%MAINDIR%\WspEventRouter.exe.config
  New Text=</configuration>
  Line Number=0
end
item: Set Variable
  Variable=COMMON
  Value=%COMMON%
  Flags=00010100
end
item: Check Configuration
  Flags=10011010
end
item: Set Variable
  Variable=MAINDIR
  Value=%MAINDIR%
  Flags=00010100
end
item: End Block
end
item: Check Configuration
  Flags=10111011
end
item: Get Registry Key Value
  Variable=STARTUPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%WIN%\Start Menu\Programs\StartUp
  Value Name=StartUp
  Flags=00000010
end
item: Get Registry Key Value
  Variable=DESKTOPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%WIN%\Desktop
  Value Name=Desktop
  Flags=00000010
end
item: Get Registry Key Value
  Variable=STARTMENUDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%WIN%\Start Menu
  Value Name=Start Menu
  Flags=00000010
end
item: Get Registry Key Value
  Variable=GROUPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%WIN%\Start Menu\Programs
  Value Name=Programs
  Flags=00000010
end
item: Get Registry Key Value
  Variable=CSTARTUPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%STARTUPDIR%
  Value Name=Common Startup
  Flags=00000100
end
item: Get Registry Key Value
  Variable=CDESKTOPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%DESKTOPDIR%
  Value Name=Common Desktop
  Flags=00000100
end
item: Get Registry Key Value
  Variable=CSTARTMENUDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%STARTMENUDIR%
  Value Name=Common Start Menu
  Flags=00000100
end
item: Get Registry Key Value
  Variable=CGROUPDIR
  Key=Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders
  Default=%GROUPDIR%
  Value Name=Common Programs
  Flags=00000100
end
item: Set Variable
  Variable=CGROUP_SAVE
  Value=%GROUP%
end
item: Set Variable
  Variable=GROUP
  Value=%GROUPDIR%\%GROUP%
end
item: Else Statement
end
item: End Block
end
item: Self-Register OCXs/DLLs
  Description English=Updating System Configuration, Please Wait...
end
item: Create Service
  Service Name=WspEventRouter
  Executable Path=%MAINDIR%\WspEventRouter.exe
  Login User=NT AUTHORITY\NetworkService
  Service Type=16
  Boot Type=2
  Error Type=0
  Display Name English=WspEventRouter
end
item: Start/Stop Service
  Service Name=WspEventRouter
end
item: Execute Program
  Pathname=%TEMP%\gacfiles.cmd
  Flags=00000010
end
item: Wizard Block
  Direction Variable=DIRECTION
  Display Variable=DISPLAY
  Bitmap Pathname=%_SMSINSTL_%\DIALOGS\TEMPLATE\WIZARD.BMP
  X Position=9
  Y Position=10
  Filler Color=8421440
  Flags=00000011
end
item: Custom Dialog Set
  Name=Finished
  Display Variable=DISPLAY
  item: Dialog
    Title Danish=%APPTITLE% Installation
    Title Dutch=Installatie van %APPTITLE%
    Title English=%APPTITLE% Installation
    Title Finnish=Asennus: %APPTITLE%
    Title French=Installation de %APPTITLE%
    Title German=Installation von %APPTITLE%
    Title Italian=Installazione di %APPTITLE%
    Title Norwegian=Installere %APPTITLE%
    Title Portuguese=Instalação de %APPTITLE%
    Title Spanish=Instalación de %APPTITLE%
    Title Swedish=Installation av %APPTITLE%
    Width=271
    Height=224
    Font Name=Helv
    Font Size=8
    item: Push Button
      Rectangle=150 187 195 202
      Variable=DIRECTION
      Value=N
      Create Flags=01010000000000010000000000000001
      Text Danish=&Udfør
      Text Dutch=&Voltooien
      Text English=&Finish
      Text Finnish=&Valmis
      Text French=Ter&miner
      Text German=&Weiter
      Text Italian=&Fine
      Text Norwegian=&Fullfør
      Text Portuguese=&Concluir
      Text Spanish=&Finalizar
      Text Swedish=S&lutför
    end
    item: Push Button
      Rectangle=105 187 150 202
      Variable=DISABLED
      Value=!
      Create Flags=01010000000000010000000000000000
      Text Danish=< &Tilbage
      Text Dutch=< &Terug
      Text English=< &Back
      Text Finnish=< &Edellinen
      Text French=< &Précédent
      Text German=< &Zurück
      Text Italian=< &Indietro
      Text Norwegian=< &Tilbake
      Text Portuguese=< &Anterior
      Text Spanish=< &Atrás
      Text Swedish=< &Föregående
    end
    item: Push Button
      Rectangle=211 187 256 202
      Variable=DISABLED
      Value=!
      Action=3
      Create Flags=01010000000000010000000000000000
      Text Danish=Annuller
      Text Dutch=Annuleren
      Text English=Cancel
      Text Finnish=Peruuta
      Text French=Annuler
      Text German=Abbrechen
      Text Italian=Annulla
      Text Norwegian=Avbryt
      Text Portuguese=Cancelar
      Text Spanish=Cancelar
      Text Swedish= Avbryt
    end
    item: Static
      Rectangle=8 180 256 181
      Action=3
      Create Flags=01010000000000000000000000000111
    end
    item: Static
      Rectangle=86 8 262 42
      Create Flags=01010000000000000000000000000000
      Flags=0000000000000001
      Name=Times New Roman
      Font Style=-24 0 0 0 700 255 0 0 0 3 2 1 18
      Text Danish=Installationen er fuldført!
      Text Dutch=De installatie is voltooid!
      Text English=Installation Completed!
      Text Finnish=Asennus on valmis.
      Text French=Installation terminée !
      Text German=Die Installation ist abgeschlossen!
      Text Italian=Installazione completata.
      Text Norwegian=Installasjonen er fullført.
      Text Portuguese=Instalação concluída!
      Text Spanish=Instalación completada.
      Text Swedish=Installationen är slutförd!
    end
    item: Static
      Rectangle=86 51 256 111
      Create Flags=01010000000000000000000000000000
      Text Danish=Installationen af %APPTITLE% er fuldført.
      Text Danish=
      Text Danish=Tryk på Udfør for at afslutte installationen.
      Text Dutch=De installatie van %APPTITLE% is geslaagd.
      Text Dutch=
      Text Dutch=Kies de knop Voltooien om de installatie af te sluiten.
      Text English=The installation of %APPTITLE% has been successfully completed.
      Text English=
      Text English=Press the Finish button to exit this installation.
      Text Finnish="Kohteen %APPTITLE% asennus on valmis. "
      Text Finnish=
      Text Finnish=Poistu asennusohjelmasta valitsemalla Valmis.
      Text French=L'installation de %APPTITLE% est réussie.
      Text French=
      Text French=Cliquez sur le bouton Terminer pour quitter ce programme d'installation.
      Text German=%APPTITLE% wurde erfolgreich installiert.
      Text German=
      Text German=Klicken Sie auf "Weiter", um die Installation zu beenden.
      Text Italian=L'installazione di %APPTITLE% è stata completata.
      Text Italian=
      Text Italian=Fare clic sul pulsante Fine per uscire dall'installazione.
      Text Norwegian=Installasjonen av %APPTITLE% er fullført.
      Text Norwegian=
      Text Norwegian=Velg Fullfør for å avslutte denne installasjonen.
      Text Portuguese=A instalação de %APPTITLE% foi concluída com êxito.
      Text Portuguese=
      Text Portuguese=Prima o botão 'Concluir' para sair desta instalação.
      Text Spanish=La instalación de %APPTITLE% terminó con éxito.
      Text Spanish=
      Text Spanish=Elija Finalizar para salir de esta instalación.
      Text Swedish=Installationen av %APPTITLE% har slutförts.
      Text Swedish=
      Text Swedish=Klicka på Slutför för att avsluta den här installationen.
    end
  end
end
item: End Block
end
item: New Event
  Name=Cancel
end
item: Include Script
  Pathname=%_SMSINSTL_%\Include\rollback.ipf
end
