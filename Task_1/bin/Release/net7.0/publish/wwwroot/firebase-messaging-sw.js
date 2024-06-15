// Give the service worker access to Firebase Messaging.
// Note that you can only use Firebase Messaging here. Other Firebase libraries
// are not available in the service worker.
importScripts('https://www.gstatic.com/firebasejs/8.10.1/firebase-app.js');
importScripts('https://www.gstatic.com/firebasejs/8.10.1/firebase-messaging.js');

// Initialize the Firebase app in the service worker by passing in
// your app's Firebase config object.
// https://firebase.google.com/docs/web/setup#config-object
firebase.initializeApp({
    apiKey: "AIzaSyBo-HzERgUhUbZsQFUzgJA1rnooBlEw87o",
    authDomain: "demopush-18ac7.firebaseapp.com",
    projectId: "demopush-18ac7",
    storageBucket: "demopush-18ac7.appspot.com",
    messagingSenderId: "935048468034",
    appId: "1:935048468034:web:07a2ece488903846e27fdb"
});

// Retrieve an instance of Firebase Messaging so that it can handle background
// messages.
const messaging = firebase.messaging();

// [START background_handler]
messaging.onBackgroundMessage(async (payload) => {
    console.log('[firebase-messaging-sw.js] Received background message ', payload);
    // Extract notification object from the payload
    const { notification } = payload;

    const notificationObject = {
        title: notification.title,
        body: notification.body
    };

    console.log(notificationObject);
    // Send payload to backend
    try {
        const response = await fetch('/Notification/StorePayload', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(notification),
        });
        if (!response.ok) {
            throw new Error('Failed to send payload to server');
        }
        console.log('Payload sent to server successfully');
    } catch (error) {
        console.error('Error sending payload to server:', error);
    }

    // Customize notification here
    const notificationTitle = 'Background Message Title';
    const notificationOptions = {
        body: 'Background Message body.',
        icon: '/firebase-logo.png'
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});
