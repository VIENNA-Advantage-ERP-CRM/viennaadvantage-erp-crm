/*
 * Globalize Culture fr-FR
 *
 * http://github.com/jquery/globalize
 *
 * Copyright Software Freedom Conservancy, Inc.
 * Dual licensed under the MIT or GPL Version 2 licenses.
 * http://jquery.org/license
 *
 * This file was generated by the Globalize Culture Generator
 * Translation: bugs found in this file need to be fixed in the generator
 */

(function( window, undefined ) {

    var Globalize;

    if ( typeof require !== "undefined" &&
        typeof exports !== "undefined" &&
        typeof module !== "undefined" ) {
        // Assume CommonJS
        Globalize = require( "globalize" );
    } else {
        // Global variable
        Globalize = window.Globalize;
    }

    Globalize.addCultureInfo( "fr-FR", "default", {
        name: "fr-FR",
        englishName: "French (France)",
        nativeName: "français (France)",
        language: "fr",
        numberFormat: {
            ",": " ",
            ".": ",",
            "NaN": "Non Numérique",
            negativeInfinity: "-Infini",
            positiveInfinity: "+Infini",
            percent: {
                ",": " ",
                ".": ","
            },
            currency: {
                pattern: ["-n $","n $"],
                ",": " ",
                ".": ",",
                symbol: "€"
            }
        },
        calendars: {
            standard: {
                firstDay: 1,
                days: {
                    names: ["dimanche","lundi","mardi","mercredi","jeudi","vendredi","samedi"],
                    namesAbbr: ["dim.","lun.","mar.","mer.","jeu.","ven.","sam."],
                    namesShort: ["di","lu","ma","me","je","ve","sa"]
                },
                months: {
                    names: ["janvier","février","mars","avril","mai","juin","juillet","août","septembre","octobre","novembre","décembre",""],
                    namesAbbr: ["janv.","févr.","mars","avr.","mai","juin","juil.","août","sept.","oct.","nov.","déc.",""]
                },
                AM: null,
                PM: null,
                eras: [{"name":"ap. J.-C.","start":null,"offset":0}],
                patterns: {
                    d: "dd/MM/yyyy",
                    D: "dddd d MMMM yyyy",
                    t: "HH:mm",
                    T: "HH:mm:ss",
                    f: "dddd d MMMM yyyy HH:mm",
                    F: "dddd d MMMM yyyy HH:mm:ss",
                    M: "d MMMM",
                    Y: "MMMM yyyy"
                }
            }
        },
        messages: {
            "Connection": "Connexion",
            "Defaults": "Défauts",
            "Login": "S'identifier",
            "File": "Fichier",
            "Exit": "Sortir",
            "Help": "Aide",
            "About": "A propos de",
            "Host": "Serveur",
            "Database": "Base de données",
            "User": "Utilisateur",
            "EnterUser": "Entrer votre code utilisateur",
            "Password": "Mot de passe",
            "EnterPassword": "Entrer le mot de passe",
            "Language": "Langue",
            "SelectLanguage": "Sélectionnez votre langue",
            "Role": "Rôle",
            "Client": "Société",
            "Organization": "Département",
            "Date": "Date",
            "Warehouse": "Stock",
            "Printer": "Imprimante",
            "Connected": "Connecté",
            "NotConnected": "Non Connecté",
            "DatabaseNotFound": "Base de données non trouvée",
            "UserPwdError": "L'utilisateur n'a pas entré de mot de passe",
            "RoleNotFound": "Rôle non trouvé",
            "Authorized": "Autorisé",
            "Ok": "Ok",
            "Cancel": "Annuler",
            "VersionConflict": "Conflit de Version:",
            "VersionInfo": "Serveur <> Client",
            "PleaseUpgrade": "SVP: mettez à jour le programme",

            //New Resource

            "Back": "arrière",
            "SelectRole": "Sélectionnez Rôle",
            "SelectOrg": "Sélectionnez organisation",
            "SelectClient": "Sélectionnez Client",
            "SelectWarehouse": "Sélectionnez Entrepôt",
            "VerifyUserLanguage": "Vérification de l'utilisateur et langage",
            "LoadingPreference": "Chargement Orientation",
            "Completed": "terminé",
            "RememberMe": "Souviens-Toi De Moi",
            "FillMandatoryFields": "Remplissez les champs obligatoires",
            "BothPwdNotMatch": "Les deux mots de passe doivent correspondre.",
            "mustMatchCriteria": "La longueur minimale du mot de passe est de 5. Le mot de passe doit avoir au moins 1 caractère majuscule, 1 caractère minuscule, un caractère spécial (@ $!% *? &) Et un chiffre. Le mot de passe doit commencer par un caractère.",
            "NotLoginUser": "L'utilisateur ne peut pas se connecter au système",
            "MaxFailedLoginAttempts": "Le compte utilisateur est verrouillé. Le nombre maximal de tentatives de connexion ayant échoué dépasse la limite définie. Veuillez contacter l'administrateur.",
            "UserNotFound": "Le nom d'utilisateur est incorrect.",
            "RoleNotDefined": "Aucun rôle défini pour cet utilisateur",
            "oldNewSamePwd": "l'ancien mot de passe et le nouveau mot de passe doivent être différents.",
            "NewPassword": "nouveau mot de passe",
            "NewCPassword": "Confirmer le nouveau mot de passe",
            "EnterOTP": "Entrez OTP",
            "WrongOTP": "OTP incorrect entré",
            "ScanQRCode": "Scannez le code avec Google Authenticator",
            "EnterVerCode": "Entrez OTP généré par votre application mobile",
            "EnterVAVerCode": "Entrez OTP reçu sur votre mobile enregistré",
            "SkipThisTime": "Passer cette fois",
            "ResendOTP": "Renvoyer OTP",
        }
});

}( this ));
