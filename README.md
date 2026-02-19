# Daycare management system

## Description

Most daycares and pre-schools in townships rely on walk-in applications and paper-based record keeping. Once a learner is enrolled, critical information such as allergies, medical conditions, authorised drop-off and pick-up contacts, and supporting documents is managed manually. This project aims to digitise these processes, providing a centralised system that simplifies administration and improves accessibility for both teachers and parents.

## Features

- User Registration and login
- JWT based authentication
- Two Factor authentication
- User management
- Applications Tracking
- Student management
- Audits and Logs
- Document Management
- Dropoff and pickup tracking
- Email Service
- PickUp notifications
- DB clean up - at the end of the year

## Prerequisites

- Docker
- MongoDB
- Visual Studio

## Technologies

- C# (.NET 8)
- ASP .NET Core Web API
- MongoDB
- MongoDB.Driver
- JWT Authentication
- Docker
- Git & Github
- Swagger
- Serilog
- Two Factor Authentication

## Project Setup and run

- Make sure you have the prerequisites installed and running.
- Clone project from github.
- configure docker-compose.yml and .env files as shown in the **Configuration** section
- Run project with docker-compose so that environment variables can be retrieved wherever they are needed.

## Configuration

There is going to be 2 files where configuration is needed .env file and docker compose

### .env

I will list the fields and what they mean.

- DefaultUser_Email - The email of the user that will be seeded on the database on 1st run.
- DefaultUser_Password - The password of the default user, one they will use to login.
- EmailSender - SMTP email address - this is going to be used to send emails - there are videos on YouTube about how to enable SMPT on your email address.
- EmailPassword - SMPT Password of the email address.
- SMTPServer - leave it as is on the file or lookup what it is if you are not using gmail.
- SMTPServerPort - leave it as is on the file or lookup what it is if you are not using gmail.
- Jwt_Key - Leave as is or generate a long id.
- Jwt_Issuer - Leave as is or can be any name you want.
- Jwt_Audience - Leave as is or can be any name you want.

### docker-compose.yml

I will list only the fields that needs changing.

- FrontEndBaseUrl - This is the base url of the front end for tasks where the backend will send an email with front-end pages e.g change password page.
- AppropriateStudentAge - This is the maximum accepted student age
- MaxStudentsAllowed - This is the maximum number of students that can be accpted to the school.
- **PickUpWorkerRunHour** and **PickUpWokerRunMinutes -** hour and minutes of when the PickUpWorkerService will run at.
- **DBCleanUpMonth** and **DBCleanUpDay -** Month and day of when the DBCleanUpWorkerService will run.

## Endpoints

I composed a google doc that will explain what each endpoint does, authentication type, how to make requests and what response will you get:

Google doc: https://docs.google.com/document/d/19UwikCejbdWuP9KGbEKySlNxIlsKL3b2sLNTqQtXJdY/edit?tab=t.0

## Future Improvements

Make it a multi-tenant app, every school that uses the app will stay in the same app and share a database so to speak instead of having different deployments for each school.

## Contacts

Contact me here if there is any issues setting up or running the application, improvements, collaborations and bug finds:

Email: paballoelisa22@gmail.com
