# 🏥 GestionCabinetMedical

Une application web complète de gestion de cabinet médical développée avec **ASP.NET Core MVC**. Ce système permet de digitaliser et simplifier les tâches administratives et médicales quotidiennes d'une clinique ou d'un cabinet.

---

## 📋 Table des Matières
- [Fonctionnalités](#-fonctionnalités)
- [Technologies Utilisées](#-technologies-utilisées)
- [Structure du Projet](#-structure-du-projet)
- [Installation et Démarrage](#-installation-et-démarrage)
- [Gestion des Rôles](#-gestion-des-rôles)
- [Aperçu](#-aperçu)

---

## 🚀 Fonctionnalités

L'application est divisée en plusieurs modules interconnectés :

### 👤 Gestion des Patients & Personnel
* **Patients :** Création, modification et suivi des dossiers patients (Numéro Sécurité Sociale, contact).
* **Médecins :** Gestion des médecins avec leurs spécialités (liés aux comptes utilisateurs).
* **Personnel Médical :** Gestion de l'équipe technique et administrative (Infirmiers, Secrétaires, etc.).

### 📁 Suivi Médical
* **Dossiers Médicaux :** Centralisation des informations (Groupe sanguin, liaison Patient-Médecin).
* **Consultations :** Historique des visites, diagnostics et observations.
* **Traitements :** Prescriptions et traitements liés aux consultations.

### 📅 Planification
* **Rendez-vous :** Système de prise de rendez-vous avec gestion des statuts (En attente, Confirmé, Annulé, Terminé).
* **Tableau de bord :** Vue d'ensemble avec statistiques en temps réel (RDV du jour, nombre de patients, etc.).

### 📦 Logistique
* **Ressources Médicales :** Gestion des stocks de matériel (seringues, compresses...) avec alertes visuelles en cas de stock critique.

### 🔒 Sécurité
* **Authentification :** Système de connexion et d'inscription sécurisé (ASP.NET Identity).
* **Autorisations :** Accès restreint selon les rôles (Admin, Médecin, Secrétaire, Patient).

---

## 🛠 Technologies Utilisées

* **Framework :** .NET 6 / .NET 7 / .NET 8 (ASP.NET Core MVC)
* **ORM :** Entity Framework Core (SQL Server)
* **Frontend :** Razor Views (.cshtml), Bootstrap 5, FontAwesome (Icônes)
* **Authentification :** ASP.NET Core Identity

---

## 📂 Structure du Projet

Les principaux contrôleurs de l'application :

* `HomeController` : Tableau de bord et statistiques.
* `PatientsController` : Gestion administrative des patients.
* `MedecinsController` : Gestion des profils médecins.
* `DossiersMedicauxController` : Cœur du dossier patient.
* `RendezVousController` : Calendrier et plannings.
* `ConsultationsController` & `TraitementsController` : Détails médicaux.
* `RessourcesMedicalesController` : Gestion de l'inventaire.
* `PersonnelsMedicauxController` : Gestion RH.

---

## 💻 Installation et Démarrage

1.  **Cloner le dépôt :**
    ```bash
    git clone [https://github.com/thelazygenius404/GestionCabinetMedical.git](https://github.com/thelazygenius404/GestionCabinetMedical.git)
    ```

2.  **Configurer la base de données :**
    * Ouvrez le fichier `appsettings.json` et vérifiez la chaîne de connexion `DefaultConnection`.
    * Ouvrez la console du gestionnaire de packages (ou le terminal) et exécutez :
    ```bash
    dotnet ef database update
    ```
    *Cela créera la base de données et les tables nécessaires.*

3.  **Lancer l'application :**
    * Ouvrez le projet dans Visual Studio ou VS Code.
    * Lancez le projet (F5 ou `dotnet run`).

---

## 🛡 Gestion des Rôles

L'application utilise des rôles pour sécuriser les routes. Assurez-vous de créer ces rôles dans votre base de données ou via un initialiseur (Seeder) :

* **ADMIN :** Accès total (Gestion utilisateurs, médecins, stocks, suppressions...).
* **MEDECIN :** Accès aux dossiers médicaux, consultations, traitements et rendez-vous.
* **SECRETAIRE :** Gestion des patients, rendez-vous et stocks.
* **PATIENT :** Accès limité (prise de rendez-vous).

---

## 📸 Aperçu

*L'interface utilise un design moderne et épuré avec des cartes Bootstrap et des indicateurs visuels.*

> **Note :** Ce projet est un système de démonstration pour la gestion de cabinet médical.

---

**Développé par [thelazygenius404](https://github.com/thelazygenius404)**
