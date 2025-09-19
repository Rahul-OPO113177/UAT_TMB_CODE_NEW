
function openTab(evt, tabId) {
    document.querySelectorAll(".tab-content").forEach(el => el.style.display = "none");
    document.querySelectorAll(".nav-tab").forEach(el => el.classList.remove("active"));
    document.querySelectorAll(".nav-tab").forEach(el => el.classList.add("inactive"));
    document.getElementById(tabId).style.display = "block";
    evt.currentTarget.parentElement.classList.add("active");
    evt.currentTarget.parentElement.classList.remove("inactive");
}

let timerInterval = null;
let seconds = 0;

function startTimer() {
    stopTimer();
    seconds = 0;
    console.log("Starting call timer...");
    timerInterval = setInterval(() => {
        seconds++;
        document.getElementById("callTimer").innerText = formatTime(seconds);
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        console.log("Stopping call timer...");
        clearInterval(timerInterval);
        timerInterval = null;
    }
    seconds = 0;
    document.getElementById("callTimer").innerText = "00:00:00";
}

function formatTime(sec) {
    const hours = Math.floor(sec / 3600).toString().padStart(2, '0');
    const minutes = Math.floor((sec % 3600) / 60).toString().padStart(2, '0');
    const seconds = (sec % 60).toString().padStart(2, '0');
    return `${hours}:${minutes}:${seconds}`;
}

function toggleAccordion() {
    const content = document.getElementById("dialer-details");
    const arrow = document.getElementById("arrowIcon");
    if (content.style.maxHeight && content.style.maxHeight !== "0px") {
        content.style.maxHeight = "0";
        arrow.style.transform = "rotate(0deg)";
    } else {
        content.style.maxHeight = content.scrollHeight + "px";
        arrow.style.transform = "rotate(180deg)";
    }
}

function toggleBreakDropdown() {
    const dd = document.getElementById("breakDropdown");
    dd.style.display = (dd.style.display === "block") ? "none" : "block";
}

function toggleScheduleDropdown() {
    const dd = document.getElementById("scheduleDropdown");
    dd.style.display = (dd.style.display === "none" || dd.style.display === "") ? "block" : "none";
}

document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search);
    const agentId = urlParams.get('empCode');

    if (!agentId) {
        console.error("Agent ID (empCode) is missing in the URL.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/ctihub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.onreconnecting((error) => {
        console.warn("SignalR reconnecting...", error);
    });

    connection.onreconnected((connectionId) => {
        console.info("✅ SignalR reconnected:", connectionId);
        connection.invoke("JoinGroup", agentId)
            .then(() => console.log("Rejoined group after reconnect"))
            .catch(err => console.error("Error rejoining group:", err));
    });

    connection.onclose((error) => {
        console.error("SignalR connection closed permanently:", error);
        const statusElem = document.getElementById("status");
        if (statusElem) statusElem.innerText = "Disconnected. Please refresh the page.";
    });



    connection.on("UserName", function (number) {

        console.log("UserName : " + number );
        const parts = number.split(".");
        const initials = parts.map(p => p.charAt(0).toUpperCase()).join("");
        const email = number + "@1point1.in";

        const usernameEl = document.getElementById("username");
        if (usernameEl) usernameEl.innerText = number;

        document.querySelectorAll(".user-avatar, .profile-avatar").forEach(el => el.innerText = initials);
        document.querySelectorAll(".profile-email").forEach(el => el.innerText = email);
    });
    connection.on("infopagedata", function (data) {
        try {
            console.log("InfoPage fields:", data);
            const fields = JSON.parse(data);

            const displayFields = fields.filter(field => field.CapturableField === "Display");
            const captureFields = fields.filter(field => field.CapturableField === "Capture");


            displayFields.forEach(field => {
                const required = field.IsRequired === "YES" ? '<span style="color:red">*</span>' : '';

                const value = field.DisplaySourceValue || '';
                $('#tab1').append(`
        <div class="field" id="${field.FieldName}_container">
            <label>${field.FieldName} ${required}</label>
            <input type="${field.FieldType}" name="${field.FieldName}" value="${value}" readonly />
        </div>
    `);
            });


            captureFields.forEach(field => {
                const required = field.IsRequired === "YES" ? '<span style="color:red">*</span>' : '';
                const isRequiredAttr = field.IsRequired === "YES" ? 'required' : '';
                let fieldHtml = `<div class="field" id="${field.FieldName}_container">
                                <label>${field.FieldName} ${required}</label>`;


                if (field.FieldType === "DROPDOWN") {
                    const isDependentTarget = captureFields.some(f => f.FieldDependetName === field.FieldName);

                    fieldHtml += `
                    <select name="${field.FieldName}" id="${field.FieldName}_dropdown" ${isRequiredAttr}>
                        <option value="">Select</option>
                        ${!isDependentTarget && field.DependentData
                            ? field.DependentData.map(option => `<option value="${option.Value}">${option.Text}</option>`).join('')
                            : ''
                        }
                    </select>
                `;
                    fieldHtml += '</div>';
                    $('#tab3').append(fieldHtml);


                    if (field.IsfieldDependent === "YES" && !isDependentTarget) {
                        $(`#${field.FieldName}_dropdown`).on('change', function () {
                            const selectedValue = $(this).val();
                            const dependentFieldName = field.FieldDependetName;
                            const dependentField = fields.find(f => f.FieldName === dependentFieldName);

                            if (dependentField) {
                                if (dependentField.DependentData) {
                                    const filteredOptions = dependentField.DependentData.filter(option => option.Value === selectedValue);
                                    const dependentDropdown = $(`#${dependentFieldName}_dropdown`);
                                    dependentDropdown.empty();
                                    dependentDropdown.append('<option value="">Select</option>');
                                    filteredOptions.forEach(option => {
                                        dependentDropdown.append(`<option value="${option.Value}">${option.Text}</option>`);
                                    });
                                }
                            }
                        });
                    }
                } else {

                    switch (field.FieldType.toLowerCase()) {
                        case "datetime":
                            fieldHtml += `<input type="datetime-local" name="${field.FieldName}" ${isRequiredAttr} />`;
                            break;

                        case "radio":
                            fieldHtml += `
                            <label><input type="radio" name="${field.FieldName}" value="Yes" ${isRequiredAttr} /> Yes</label>
                            <label><input type="radio" name="${field.FieldName}" value="No" ${isRequiredAttr} /> No</label>
                        `;
                            break;

                        case "checkbox":
                            fieldHtml += `<input type="checkbox" name="${field.FieldName}" ${isRequiredAttr} />`;
                            break;

                        default:
                            fieldHtml += `<input type="text" name="${field.FieldName}" ${isRequiredAttr} />`;
                            break;
                    }
                    fieldHtml += '</div>';
                    $('#tab3').append(fieldHtml);
                }


                if (field.Isinitaldisplay === "No" && field.DisplaySource && field.DisplaySourceValue) {
                    const controlField = fields.find(f => f.FieldName === field.DisplaySource);
                    if (controlField) {
                        const dropdownId = `#${controlField.FieldName}_dropdown`;

                        $(`#${field.FieldName}_container`).hide();


                        $(dropdownId).on('change', function () {
                            const selectedText = $(this).find('option:selected').text();


                            if (selectedText === field.DisplaySourceValue) {
                                $(`#${field.FieldName}_container`).show();
                            } else {
                                $(`#${field.FieldName}_container`).hide();
                            }
                        });
                    }
                }
            });

              BindDispositionAndSubDispo();

        } catch (error) {
            console.error("Error processing the InfoPage data:", error);
        }
    });


    connection.on("UpdatePhoneInput", function (number) {
        let last10 = number.slice(-10);
        document.getElementById("phoneInput").value = last10;
    });
    connection.on("AutoWrap", function (number) {

        connection.on("AutoWrap", function (number) {
            Swal.fire({
                icon: 'success',
                title: 'Record Saved Successfully',
                text:  + number
            }).then(result => {
                
            });
        });

    });


    connection.on("ReceiveStatus", handleStatusUpdate);
    connection.on("ReceiveAttachedDataUserEvent", handleAttachedDataUserEvent);
    connection.on("ReceiveAttachedData", handleAttachedData);

    async function startConnection() {
        try {
            await connection.start();
            console.log("SignalR connected");
            await connection.invoke("JoinGroup", agentId);
            await updateStatus();
            startTimer();
        } catch (err) {
            console.error("SignalR connection error:", err);
            setTimeout(startConnection, 5000);
        }
    }
    startConnection();
});


function validateDate() {
    const selectedDate = document.getElementById('callBackDateOutcome').value;
    const today = new Date();
    const selectedDateObj = new Date(selectedDate);

    if (selectedDateObj < today) {
        alert("The selected date cannot be in the past. Please choose a future date.");
        document.getElementById('callBackDateOutcome').value = ""; 
    }
}
function handleStatusUpdate(message) {
    const statusElem = document.getElementById("status");
    if (statusElem) statusElem.innerText = message;
    const msg = message.toLowerCase();

    $("#dialGroup, #hold, #unhold, #confo, #merge, #party").hide();
    $("#break, #getnext").addClass("disabled").css({ "pointer-events": "none", "opacity": "0.5" });

    if (msg.includes("waiting")) {
        $("#break").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });
        $("#dialGroup").css("display", "flex");
        $("#getnext").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });
    }

    if (msg.includes("talking") || msg.includes("established") || msg.includes("outbound call established") || msg.includes("inbound call established")) {
        $("#dialGroup").hide();
        $("#hold, #confo, #merge, #party").css("display", "flex");
        $("#getnext").addClass("disabled").css({ "pointer-events": "none", "opacity": "0.5" });
    }

    if (msg.includes("hold")) $("#unhold").css("display", "flex");

    if (msg.includes("wraping")) {
        $("#dialGroup").css("display", "flex");
        $("#hold, #unhold, #confo, #merge, #party").hide();
        $("#getnext").addClass("disabled").css({ "pointer-events": "none", "opacity": "0.5" });
        $("#break").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });
    }

    if (["talking", "wraping", "dialing", "waiting", "established", "outbound call established", "inbound call established"].some(s => msg.includes(s))) {
        stopTimer();
        startTimer();
    } else if (["call ended", "disconnected", "hangup", "idle"].some(s => msg.includes(s))) {
        stopTimer();
    }

    if (msg.includes("break")) {
        stopTimer();
        startTimer();
    }
}

function handleAttachedDataUserEvent(data) {
    document.getElementById("batchId").innerText = data.Batch_id || "-";
    document.getElementById("mode").innerText = data.InteractionSubtype || "-";
    document.getElementById("mycode").innerText = data.TMasterID || "-";
    document.getElementById("eventType").innerText = data.GSW_USER_EVENT || "-";
    document.getElementById("campaignName").innerText = data.GSW_CAMPAIGN_NAME || "-";
}

function handleAttachedData(data) {
    if (data && typeof data === "object") {
        document.getElementById("campaignName").textContent = data.RTargetObjectSelected || "-";
        document.getElementById("mode").textContent = data.RStrategyName || "-";
        document.getElementById("objectId").textContent = data.RTargetAgentSelected || "-";
    }
}


function callAPI(url, method = 'POST', data = {}) {
    fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    }).then(async res => {
        if (!res.ok) {
            const errorText = await res.text();
            if (errorText === "Agent ready for next call." || errorText === "Agent successfully logged out") {
                Swal.fire({ icon: 'success', title: 'Success', text: errorText }).then(result => {
                    if (errorText.includes("logged out") && result.isConfirmed) location.href = "/Pages/login.html";
                });
            } else {
                throw new Error(errorText || "API call failed");
            }
        } else console.log("✅ API call successful.");
    }).catch(err => Swal.fire({ icon: 'warning', title: 'Oops...', text: err.message || "Something went wrong!" }));
}

function makeCall() {
    const phone = document.getElementById("phoneInput").value.trim();
    if (!/^\d{10}$/.test(phone)) { alert("Phone number must be exactly 10 digits."); return; }
    callAPI("/api/Genesys/makecall", "POST", { phone });
}

function openConferencePrompt() {
    Swal.fire({
        title: 'Enter conference number',
        input: 'text',
        inputPlaceholder: 'e.g. 7058053821',
        showCancelButton: true,
        confirmButtonText: 'Join',
        inputValidator: value => (!value || !/^\d{10}$/.test(value)) ? 'Enter valid 10-digit number!' : null
    }).then(result => {
        if (result.isConfirmed) callAPI('/api/Genesys/conference', 'POST', { number: result.value });
    });
}

function sendBreak(reasonCode) {
    if (!reasonCode) { Swal.fire({ icon: 'warning', title: 'No break reason selected' }); return; }
    callAPI('/api/Genesys/break', 'POST', { reasonCode });
}

function sendTransfer(selectElement) {
    const route = selectElement.value;
    if (!route) { Swal.fire({ icon: 'warning', title: 'No transfer route selected' }); selectElement.selectedIndex = 0; return; }
    callAPI('/api/Genesys/transfer', 'POST', { route });
    setTimeout(() => selectElement.selectedIndex = 0, 300);
}
var dispo = [];

function BindDispositionAndSubDispo() {
    const urlParams = new URLSearchParams(window.location.search);
    const agentId = urlParams.get('empCode'); 

    if (!agentId) {
        console.error("empCode not found in URL.");
        return;
    }

    $.ajax({

        url: '/api/InfoPage/GetDispositions',
        type: 'GET',
        data: { empCode: agentId },
        success: function (response) {
            const dispositions = response.dispositions || [];
            dispo = response;
            const dispositionSelect = $('#disposition');
            dispositionSelect.empty();
            dispositionSelect.append('<option value="">-- Select Disposition --</option>');

            dispositions.forEach(disposition => {
                console.log(disposition.disP_TYPE); 
                dispositionSelect.append(
                    `<option value="${disposition.id}" data-disp-type="${disposition.disP_TYPE}">${disposition.name}</option>`
                );
            });
        },
        error: function (error) {
            console.error("Error fetching disposition data:", error);
        }
    });
}
function updateSubDisposition() {
    const dispoId = document.getElementById("disposition").value;
    const subDispoSelect = document.getElementById("subDisposition");
    const callBackDateDiv = document.getElementById("callBackDateDiv");
    console.log(JSON.stringify(dispo));

    subDispoSelect.innerHTML = '<option value="">-- Select Sub Disposition --</option>';

    if (!callBackDateDiv) {
        console.error("Call Back Date Div not found!");
        return;
    }


    callBackDateDiv.style.display = "none";

    if (dispoId) {
    
        const selectedOption = document.querySelector(`#disposition option[value="${dispoId}"]`);
        const dispType = selectedOption ? selectedOption.getAttribute("data-disp-type") : null;


        const selectedDisposition = dispo.dispositions.find(d => d.id === parseInt(dispoId));

        if (selectedDisposition) {
            const subDispositions = selectedDisposition.subDispositions;

            const uniqueSubDispositions = new Set(subDispositions.map(sub => JSON.stringify(sub)));

            uniqueSubDispositions.forEach(sub => {
                const subObj = JSON.parse(sub);
                const option = document.createElement("option");
                option.value = subObj.id;
                option.textContent = subObj.name;
                subDispoSelect.appendChild(option);
            });

       
            console.log("Disposition Type: " + dispType);

            if (dispType === "PCB" || dispType === "CCB") {
           
                callBackDateDiv.style.display = "flex";
            }
        }
    }
}
function toggleCallBackDateField() {
    const dispoSelect = document.getElementById("disposition");
    const selectedDisposition = dispoSelect.options[dispoSelect.selectedIndex];
    const dispType = selectedDisposition ? selectedDisposition.getAttribute('data-disp-type') : '';

    const callBackDateField = document.getElementById("callBackDateOutcome").parentElement;
    if (dispType === "PCB" || dispType === "CCB") {
        callBackDateField.style.display = "flex";
    } else {
        callBackDateField.style.display = "none";
    }
}


document.getElementById("disposition").addEventListener("change", function () {
    updateSubDisposition();
    toggleCallBackDateField(); 
});
async function submitDisposition() {
    const dispoId = parseInt(document.getElementById("disposition").value);
    const subDispoId = parseInt(document.getElementById("subDisposition").value);
    const username = document.getElementById("remark").value;
    const address = document.getElementById("remark").value;
    const callBackDateValue = document.getElementById("callBackDateOutcome").value;
    const callBackDate = new Date(callBackDateValue).toISOString();

    console.log("Request Body: ", JSON.stringify({
        dispositionId: dispoId,
        subDispositionId: subDispoId,
        username,
        address,
        callBackDate
    }));

    try {
        const response = await fetch('/api/Genesys/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                dispositionId: dispoId,
                subDispositionId: subDispoId,
                username:username,
                address: address,
                callBackDate: callBackDate
            })
        });

        if (response.ok) {
            const result = await response.json();
            Swal.fire({ icon: 'success', title: 'Success', text: result.message });
        } else {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to submit disposition.' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Exception', text: error?.message || 'An unexpected error occurred.' });
    }
}
