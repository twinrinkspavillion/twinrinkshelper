﻿if (!Vue)
    throw new Error("VueJS not found!");
if (!$)
    throw new Error("JQuery not found!");

if ('serviceWorker' in navigator) {
    // Use the window load event to keep the page load performant
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/m/mobileProgressiveApp-serviceWorker.js');
    });
}


var app = new Vue({
    el: '#app',
    data: {
        alert: {
            showAlert: false,
            alertText: ""
        },
        showLoading: false,
        selectedTeam: null,
        teams: [],
        eventsWeeks: []
    },
    created: function () {
        let v = this;

        v.showLoading = true;

        $.ajax({
            dataType: "json",
            url: '/api/mobile/GetTeams',
            type: 'GET',
            success: function (data) {
                v.teams = data;
                v.showLoading = false;

                if (localStorage.getItem('selected-team')) {
                    v.selectedTeam = localStorage.getItem('selected-team');
                }

            },
            error: function (xhr, ajaxOptions, thrownError) {
                console.warn('Error in /api/mobile/GetTeams');

                v.showLoading = false;

                v.alert.showAlert = true;
                v.alert.alertText = 'Error fetching teams';

            }
        });
    },
    methods: {
        onRefreshClick: function () {    
            this.loadSchedule(this.selectedTeam);
        },
        loadSchedule: function (team) {        
            var v = this;

            if (team === null) {

                v.eventsWeeks = [];

                return;
            }

            v.alert.showAlert = false;

            v.showLoading = true;

            $.ajax({
                dataType: "json",
                url: '/api/mobile/GetSchedule?team=' + team,
                type: 'GET',
                success: function (data) {
                    v.eventsWeeks = data;
                    v.showLoading = false;
                  
                    document.title = v.selectedTeam + ' Schedule';
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    console.warn('Error in /api/mobile/GetSchedule');

                    v.showLoading = false;
                    v.eventsWeeks = [];

                    v.alert.showAlert = true;
                    v.alert.alertText = 'Error fetching schedule';

                }
            });
        }

    },
    watch: {
        selectedTeam: function (team) {

            localStorage.setItem('selected-team', team);

            this.loadSchedule(team);
        }
    }

});
Vue.config.devtools = true;