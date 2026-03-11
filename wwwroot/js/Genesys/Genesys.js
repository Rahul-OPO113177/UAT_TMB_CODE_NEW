let timerInterval = null;
let seconds = 0;
let startTime;
let selectedPartyId = null;
let entityId = null;
let TypeOfcustomer = null;
let partynumber = null;
let subdisptypesave = null;
let waitingInterval = null;
let captureFields;
let oracleTab = null;
function openTab(evt, tabId) {
    document.querySelectorAll(".tab-content").forEach(el => el.style.display = "none");
    document.querySelectorAll(".nav-tab").forEach(el => el.classList.remove("active"));
    document.querySelectorAll(".nav-tab").forEach(el => el.classList.add("inactive"));
    document.getElementById(tabId).style.display = "block";
    evt.currentTarget.parentElement.classList.add("active");
    evt.currentTarget.parentElement.classList.remove("inactive");
}

function showWarning_New(message) {
    Swal.fire({
        icon: 'warning',
        title: 'Warning',
        text: message, InfoPage ,
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        background: '#fff7ed',
        color: '#92400e',
        iconColor: '#f59e0b',
        customClass: {
            popup: 'shadow-lg rounded-xl'
        }
    });
}

function startTimer() {
    stopTimer(); 
    startTime = Date.now();
    console.log("Starting call timer..." + new Date().toLocaleString());

    timerInterval = setInterval(() => {
        const elapsedMs = Date.now() - startTime;
        const elapsedSeconds = Math.floor(elapsedMs / 1000);
        document.getElementById("callTimer").innerText = formatTime(elapsedSeconds);
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        console.log("Stopping call timer..." + new Date().toLocaleString());
        clearInterval(timerInterval);
        timerInterval = null;
    }
    document.getElementById("callTimer").innerText = "00:00:00";
    startTime = null;
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
        console.info("SignalR reconnected:", connectionId);
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

        console.log("UserName : " + number);
        const parts = number.split(".");
        const initials = parts.map(p => p.charAt(0).toUpperCase()).join("");
        const emails == number + "@1point1.in";
        const usernameEl = document.getElementById("username");
        if (usernameEl) usernameEl.innerText = number;

        document.querySelectorAll(".user-avatar, .profile-avatar").forEach(el => el.innerText = initials);
        document.querySelectorAll(".profile-email").forEach(el => el.innerText = email);
    });


    connection.on("history", function (data) {
        try {
            console.log("History data received:", data);

            if (typeof data === 'string') {
                try {
                    data = JSON.parse(data);
                    console.log("Parsed history data:", data);
                } catch (error) {
                    console.error("Error parsing data:", error);
                    return;
                }
            }

            const container = document.getElementById('historyContainer');
            if (!container) {
                console.error("Container not found!");
                return;
            }

            container.innerHTML = '';

            const table = document.createElement('table');
            table.style.width = '100%';
            table.style.borderCollapse = 'collapse';
            table.style.fontSize = '13px';

            const thead = document.createElement('thead');
            const headerRow = document.createElement('tr');
            headerRow.style.background = '#f1f1f1';

            const headers = ['Type', 'Date', 'Time', 'Disposition', 'Sub Disposition', 'Remark', 'Call Back / Follow-up Date'];

            headers.forEach(header => {
                const th = document.createElement('th');
                th.style.padding = '8px';
                th.style.border = '1px solid #ccc';
                th.style.textAlign = 'left';
                th.textContent = header;
                headerRow.appendChild(th);
            });

            thead.appendChild(headerRow);
            table.appendChild(thead);

            const tbody = document.createElement('tbody');
            if (!Array.isArray(data) || data.length === 0) {
                console.log("No history data available.");
                return;
            }

            data.forEach(function (historyItem) {
                const row = document.createElement('tr');

                const rowData = [
                    historyItem.Type || 'N/A',
                    formatDate(historyItem.Date) || 'N/A',
                    formatTime(historyItem.Time) || 'N/A',
                    historyItem.Disposition || '--',
                    historyItem.SubDisposition || '--',
                    historyItem.REMARKS || '--',
                    historyItem.callbacktime || '--'
                ];

                rowData.forEach(function (cellData) {
                    const td = document.createElement('td');
                    td.style.padding = '8px';
                    td.style.border = '1px solid #ccc';
                    td.style.textAlign = 'left';
                    td.textContent = cellData;
                    row.appendChild(td);
                });

                tbody.appendChild(row);
            });

            table.appendChild(tbody);

            container.appendChild(table);

        } catch (error) {
            console.error("An error occurred while processing history data:", error);
        }
    });

    function formatDate(dateStr) {
        try {
            if (!dateStr) return 'N/A';
            const [day, month, year] = dateStr.split(' ')[0].split('-');
            const time = dateStr.split(' ')[1];
            const formattedDate = `${year}-${month}-${day}T${time}`;

            const date = new Date(formattedDate);
            if (isNaN(date.getTime())) return 'Invalid Date';
            return date.toLocaleDateString();
        } catch (error) {
            console.error("Error formatting date:", error);
            return 'Invalid Date';
        }
    }

    function formatTime(timeStr) {
        if (!timeStr) return 'N/A';
        const date = new Date('1970-01-01T' + timeStr + 'Z');
        if (isNaN(date.getTime())) return 'Invalid Time';
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    connection.on("PlayRingtone", function (audioFilePath) {
        var audio = new Audio(audioFilePath);
        audio.play().catch(function (error) {
            console.error("Failed to play audio:", error);
        });
    });


    connection.on("IframeEntity", function (data) {

        console.log("IframeEntity Data:", data);

        entityId = data.items[0]._entity ?? "";
        selectedPartyId = data.items[0].partyId ?? "";
        partynumber = data.items[0].partyNumber ?? "";
      

        if (!data || !data.items || data.items.length === 0) {
            $("#entityframe").html(`
            <div style="
                padding:10px;
                margin-top:10px;
                font-size:16px;
                font-weight:bold;
                color:#a00;
            ">
                No data found
            </div>
        `);
            return;
        }

      

    
        let tableHtml = `
        <table id="entityTable" style="width:100%; border-collapse:collapse; margin-top:10px;">
            <thead>
                <tr style="background:#f0f0f0; text-align:left;">
                    <th style="padding:8px; border:1px solid #ccc;">Entity</th>
                    <th style="padding:8px; border:1px solid #ccc;display:none;">Party ID</th>
                    <th style="padding:8px; border:1px solid #ccc;">
                        ${data.items[0]._entity === "Account" ? "Organization Name" : "Customer Name"}
                    </th>
                    <th style="padding:8px; border:1px solid #ccc;display:none;">Party Number</th>
                </tr>
            </thead>
            <tbody>
    `;

        data.items.forEach(item => {

            


            const displayName = item._entity === "Account"
                ? item.organizationName ?? ""           
                : `${item.personFirstName ?? ""} ${item.personLastName ?? ""}`.trim();  

            tableHtml += `
            <tr class="entity-row"
                data-partyid="${item.partyId}"
                data-entity="${item._entity}"
                data-partynumber="${item.partyNumber}"
                data-firstname="${item.personFirstName}"
                data-lastname="${item.personLastName}"
                style="cursor:pointer;">
                
                <td style="padding:8px; border:1px solid #ccc;">${item._entity}</td>
                <td style="padding:8px; border:1px solid #ccc;display:none;">${item.partyId}</td>
                <td style="padding:8px; border:1px solid #ccc;">${displayName}</td>
                <td style="padding:8px; border:1px solid #ccc;display:none;">${item.partyNumber}</td>
            </tr>
        `;
        });

        tableHtml += `
            </tbody>
        </table>
    `;

    
        $("#entityframe").html(tableHtml);

        console.log("tableHtml:", tableHtml);
        
        $('#entityframe').off("click").on('click', '.entity-row', function () {
            let partyId = $(this).data('partyid');
            let entity = $(this).data('entity');
            let partyNumberid = $(this).data('partynumber');


            selectedPartyId = partyId;
            entityId = entity;
         

            
            openOracleRecord(entity, partyId, partyNumberid);
        });
    });

    connection.on("Registeredcustno", function (data) {
        try {

            const result = typeof data === "string" ? JSON.parse(data) : data;

            console.log("Registeredcustno data received:", result);

            if (result.desc) {
                showSuccess(result.desc);
            }
            else if (result.details) {
                showWarning_Required("Registered Customer API call failed");
            }
            else if (result.error) {
                showWarning_Required("Registered Customer API call failed");
            }

        } catch (error) {
            console.error("Error processing the Registeredcustno data:", error);
        }
    });



    connection.on("infopagedata", function (data) {
        try {
            console.log("InfoPage fields:", data);
            const fields = JSON.parse(data);

           
            const tab1Container = $('#tab1').find('div[style="background:#fff; padding:15px; border-radius:8px; margin-bottom:20px; box-shadow:0 2px 6px rgba(0,0,0,0.1);"]');
          
          

      
                tab1Container.prepend(`
                <div style="width:971px;margin-bottom:48px;" id="entityframe"></div><br><br></div>
            `);
         

            if ($('#phoneSearch').length === 0) {
                tab1Container.prepend(`
        <div style="display:flex; flex-direction:row; align-items:center; gap:20px; margin-bottom:20px;">
            <input id="phoneSearch"
                style="flex:1; padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;"
                type="password"
                name="extra"
                placeholder="Enter The Number"
                oncopy="return false"
                onpaste="return false"
                oncut="return false"
                maxlength="10"
                oninput="this.value=this.value.replace(/[^0-9]/g,'');"
            />
            <button type="button"
                style="padding:6px 14px; font-size:13px; border:none; border-radius:4px; background:#007bff; color:#fff; cursor:pointer; font-weight:500; box-shadow:0 1px 3px rgba(0,0,0,0.2); margin-left:10px;"
                onclick="searchPhoneNumber()">
                Open Cx
            </button>
        </div>
    `);
            }

            if (fields.error) {
                console.log("No data available");
                return;
            }

            const tab3Container = $('#tab3').find('div[style*="display:flex"][style*="flex-wrap:wrap"][style*="gap:15px"][style*="margin-top:10px"]');

            const parsed = JSON.parse(data);
            if (parsed.error) {
                console.log("Nodata");
            }
            else {
                const displayFields = fields.filter(field => field.CapturableField === "Display");
               
                 captureFields = fields.filter(field => field.CapturableField === "Capture");

                displayFields.forEach(field => {
                    const required = field.IsRequired === "YES" ? '<span style="color:red">*</span>' : '';

                    const value = field.DisplaySourceValue || '';
                    tab1Container.append(`
                    <div style="display:flex; flex-direction:column; flex:1 1 200px;" id="${field.FieldName}_container">
                        <label style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">${field.FieldName} ${required}</label>
                        <input  style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="${field.FieldType}" name="${field.FieldName}" value="${value}" readonly />
                    </div>
                    
                `);

                    
                });

                tab1Container.css({
                    "display": "flex",
                    "flex-wrap": "wrap",
                    "gap": "15px"
                });

                captureFields.forEach(field => {
                    const required = field.IsRequired === "YES" ? '<span style="color:red">*</span>' : '';
                    const isRequiredAttr = field.IsRequired === "YES" ? 'required' : '';
                    let fieldHtml = `<div style="display:flex; flex-direction:column; flex:1 1 200px;" id="${field.FieldName}_container">
                                <label style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;" >${field.FieldName} ${required}</label>`;
                   
                    if (field.FieldType === "DROPDOWN") {
                        const isDependentTarget = captureFields.some(f => f.FieldDependetName === field.FieldName);

                      

                        fieldHtml += `
                    <select  style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" name="${field.FieldName}" id="${field.FieldName}_dropdown" ${isRequiredAttr}>
                        <option value="">Select</option>
                        ${!isDependentTarget && field.DependentData
                                ? field.DependentData.map(option => `<option value="${option.Value}">${option.Text}</option>`).join('')
                                : ''
                            }
                    </select>
                `;
                        fieldHtml += '</div>';
                        tab3Container.append(fieldHtml);


                        if (field.IsfieldDependent === "YES" && !isDependentTarget) {
                            $(`#${field.FieldName}_dropdown`).on('change', function () {
                                const selectedValue = $(this).val();

                                const selectedText = $(this).find("option:selected").text(); // text

                                TypeOfcustomer = selectedText;

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
                                fieldHtml += `<input style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="datetime-local" name="${field.FieldName}" ${isRequiredAttr} />`;
                                break;

                            case "radio":
                                fieldHtml += `
                            <label><input style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="radio" name="${field.FieldName}" value="Yes" ${isRequiredAttr} /> Yes</label>
                            <label><input style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="radio" name="${field.FieldName}" value="No" ${isRequiredAttr} /> No</label>
                        `;
                                break;

                            case "checkbox":
                                fieldHtml += `<input style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="checkbox" name="${field.FieldName}" ${isRequiredAttr} />`;
                                break;

                            default:
                                let inputType = field.FieldName === "Mobile_Number" ? "password" : "text";

                                fieldHtml += `<input style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="${inputType}" name="${field.FieldName}" ${isRequiredAttr} />`;
                              
                                break;
                        }
                        fieldHtml += '</div>';
                        tab3Container.append(fieldHtml);
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
            }

            const htmlContent = `
                                <div style="display:flex; flex-direction:column; flex:1 1 200px;">
                                    <label for="disposition" style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">Disposition  <span style="color:red;">*</span> </label>
                                    <select id="disposition" onchange="updateSubDisposition()"
                                            style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;">
                                        <option value="">-- Select Disposition --</option>
                                    </select>
                                </div>

                                <div id="SubDispositiondiv" style="display:flex;  display: none; flex-direction:column; flex:1 1 200px;">
                                    <label for="subDisposition" style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">Sub Disposition  <span style="color:red;">*</span></label>
                                    <select id="subDisposition"  onchange="updateSubSubDisposition()"
                                            style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;">
                                        <option value="">-- Select Sub Disposition --</option>
                                    </select>
                                </div>

                                   <div id="SubSubDispo" style="display:flex; display: none; flex-direction:column; flex:1 1 200px;">
                                    <label for="subSubDisposition" style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">Sub Sub Disposition  <span style="color:red;">*</span></label>
                                    <select id="subSubDisposition"
                                            style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;">
                                        <option value="">-- Select Sub  SubDisposition --</option>
                                    </select>
                                </div>
                                    <div id="callBackDateDiv" style="display: none; flex-direction: column; flex: 1 1 200px;">
                                    <label for="callBackDateOutcome" style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">
                                        Call Back Date  <span style="color:red;">*</span>
                                    </label>
                                    <input id="callBackDateOutcome"  style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px;" type="datetime-local"
                                          onchange="validateDate()">
                                </div>
                                <div style="display:flex; flex-direction:column; flex:1 1 200px;">
                                    <label for="remark" style="font-size:13px; margin-bottom:3px; font-weight:500; color:#444;">Remark  <span style="color:red;">*</span></label>
                                 <textarea id="remark" placeholder="Enter Remark"
    style="padding:5px 8px; font-size:13px; border:1px solid #ccc; border-radius:4px; width:100%; height:100px; resize:vertical;"></textarea>

                                </div>`;

            tab3Container.append(htmlContent);
            BindDispositionAndSubDispo();



        } catch (error) {
            console.error("Error processing the InfoPage data:", error);
        }

     
    });


    function openOracleRecord(entity, partyId, partyNumber) {
        
        const baseUrl = "https://tmbhcm-iacciz-test.fa.ocs.oraclecloud.com/fscmUI/redwood/cx-sales/application/container";
        let url = "";

        switch (entity) {
            case "Account":
                url = `${baseUrl}/accounts/accounts-detail?view=foldout&puid=${partyNumber}`;
                break;

            case "Contact":
                url = `${baseUrl}/contacts/contacts-detail?view=foldout&puid=${partyNumber}`;
                break;

            default:
                console.error("No URL defined for entity:", entity);
                return;
        }

       
        if (oracleTab && !oracleTab.closed) {
            oracleTab.location.href = url;
            oracleTab.focus();
        } else {
            oracleTab = window.open(url, "_blank");
        }
    }

    connection.on("UpdatePhoneInput", function (number) {
        if (number == null) {
            console.warn("Received null or undefined number");

        }
        else {
            try {
                let last10 = number.slice(-10);
                let inputEl = document.getElementById("phoneInput");
                if (inputEl) {
                    inputEl.value = last10;

                } else {
                    console.warn("phoneInput element not found.");
                }
            } catch (error) {
                console.error("Error in UpdatePhoneInput handler:", error);
            }

        }

    });


   
    connection.on("opencxpage", (data) => {
        try {
            const result = typeof data === "string" ? JSON.parse(data) : data;
            const partyNumber = result?.PartyNumber;

            if (!partyNumber) {
                console.error("PartyNumber not found in data");
                return;
            }

            const entity = TypeOfcustomer === "Non-Individual" ? "Account" : "Contact";

            openOracleRecord(entity, null, partyNumber);

        } catch (error) {
            console.error("Error processing opencxpage data:", error);
        }
    });

   

    //connection.on("UpdatePhoneInput", function (number) {

    //    console.log("Received number:", number);

    //    if (!number) {
    //        console.warn("Received null or undefined number");
    //        return;
    //    }

    //    try {

    //        let cleanNumber = number.toString().replace(/\D/g, "");


    //        let last4 = cleanNumber.slice(-4);


    //        let masked = "******" + last4;

    //        let inputEl = document.getElementById("phoneInput");

    //        if (inputEl) {
    //            inputEl.value = masked;
    //            console.log("Textbox updated with:", masked);
    //        } else {
    //            console.warn("phoneInput element not found.");
    //        }

    //    } catch (error) {
    //        console.error("Error in UpdatePhoneInput handler:", error);
    //    }
    //});


    
    connection.on("AutoWrap", function (number) {

        connection.on("AutoWrap", function (number) {
            Swal.fire({
                icon: 'success',
                title: 'Record Saved Successfully',
                text: + number
            }).then(result => {

            });
        });

    });

    setInterval(async () => {
        if (connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("Heartbeat", agentId)
                .then(() => console.log("Heartbeat sent"))
                .catch(err => console.warn("Heartbeat failed:", err));
        } else if (connection.state === signalR.HubConnectionState.Disconnected) {
            console.warn("Connection is disconnected. Attempting manual reconnect...");

            try {
                await connection.start();
                console.log("Manual reconnect successful");

                await connection.invoke("JoinGroup", agentId);
                console.log("Rejoined group after manual reconnect");

            } catch (err) {
                console.error("Manual reconnect failed:", err);
            }
        } else {
          
            console.warn("Connection is not ready. Current state:", connection.state);
        }
    }, 5 * 60 * 1000); 





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
function updateStatusText(message) {
    const statusElem = document.getElementById("status");
    if (!statusElem) return;

    statusElem.innerText = message;
    const msg = message.toLowerCase();

    let color = "black"; 

    if (msg.includes("waiting")) {
        color = "gray";
    } else if (msg.includes("talking") || msg.includes("established") || msg.includes("outbound call established") || msg.includes("inbound call established")) {
        color = "green";
    } else if (msg.includes("hold")) {
        color = "blue";
    } else if (msg.includes("wraping")) {
        color = "purple";
    } else if (msg.includes("break")) {
        color = "red";
    } else if (msg.includes("logout")) {
        color = "gray";
    } else if (["call ended", "disconnected", "hangup", "idle"].some(s => msg.includes(s))) {
        color = "darkred";
    }

    statusElem.style.color = color;
}

function validateDate() {
    const selectedDate = document.getElementById('callBackDateOutcome').value;
    const today = new Date();
    const selectedDateObj = new Date(selectedDate);

    if (selectedDateObj < today) {
        showWarning_New("The selected date cannot be in the past. Please choose a future date.");
        document.getElementById('callBackDateOutcome').value = "";
    }
}
function handleStatusUpdate(message) {
    const statusElem = document.getElementById("status");
    if (statusElem) statusElem.innerText = message;
    const msg = message.toLowerCase();

    stopTimer();
    startTimer();

    updateStatusText(message);
    $("#dialGroup, #hold, #unhold, #confo, #merge, #party").hide();
    $("#break, #getnext").addClass("disabled").css({ "pointer-events": "none", "opacity": "0.5" });

    if (msg.includes("waiting")) {
        clearFeilds();
        $("#break").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });
        $("#dialGroup").css("display", "flex");
        $("#getnext").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });
        $("#btnReadys").removeClass("disabled").css({ "pointer-events": "auto", "opacity": "1" });

        
    }

    if (msg.includes("waiting")) {

        stopTimer();
        startTimer();
    }

    if (msg.includes("talking") || msg.includes("established") || msg.includes("outbound call established") || msg.includes("inbound call established")) {
        $("#dialGroup").hide();
        $("#hold, #confo, #merge, #party").css("display", "flex");
        $("#getnext").addClass("disabled").css({ "pointer-events": "none", "opacity": "0.5" });
    }

    if (msg.includes("hold")) $("#unhold").css("display", "flex");

  
    if (msg.includes("dialing")) {
        $("#party").css("display", "flex");
    }


    if (["talking", "wraping","hold" ,  "dialing", "waiting", "established", "outbound call established", "inbound call established"].some(s => msg.includes(s))) {
        stopTimer();
        startTimer();
    } else if (["call ended", "disconnected", "hangup", "idle"].some(s => msg.includes(s))) {
        stopTimer();
    }




    if (msg.includes("break")) {
        stopTimer();
        startTimer();
    }
    if (msg.includes("logout")) {

       

        Swal.fire({
            title: 'Logged Out',
            text: 'Agent is logged out successfully.',
            icon: 'success',
            showConfirmButton: false,
            timer: 2000,
            willClose: () => {

                window.location.href = "/Pages/LogIn.html";
            }
        });
    }

}

function handleAttachedDataUserEvent(data) {

    document.getElementById("batchId").innerText = data.Batchid || "-";
    document.getElementById("mode").innerText = data.InteractionSubtype || "-";
    document.getElementById("mycode").innerText = data.TMasterID || "-";
    document.getElementById("eventType").innerText = data.GSW_USER_EVENT || "-";
    document.getElementById("campaignName").innerText = data.GSW_CAMPAIGN_NAME || "-";
}

function handleAttachedData(data) {
    //const langInput = document.querySelector('input[name="LANG"]');
    //const menu1Input = document.querySelector('input[name="Menu1"]');
    //const menu2Input = document.querySelector('input[name="Menu2"]');

    //if (langInput) langInput.value = data.Cust_Inpute1 || "";
    //if (menu1Input) menu1Input.value = data.Cust_Inpute2 || "";
    //if (menu2Input) menu2Input.value = data.Cust_Inpute3 || "";
    //console.log("Attached data will be : " + JSON.stringify(data));

    //const campaignElem = document.getElementById("campaignName");
    //if (campaignElem) campaignElem.textContent = data.RTargetObjectSelected || "-";

    //const modeElem = document.getElementById("mode");
    //if (modeElem) modeElem.textContent = data.RStrategyName || "-";

    //const objectElem = document.getElementById("objectId");
    //if (objectElem) objectElem.textContent = data.RTargetAgentSelected || "-";
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
        } else {

        }
    }).catch(err => Swal.fire({ icon: 'warning', title: 'Oops...', text: err.message || "Something went wrong!" }));
}

function clearFeilds() {
    $('#tab1').empty();
    var newDiv = $('<div>', {
        style: "background:#fff; padding:15px; border-radius:8px; margin-bottom:20px; box-shadow:0 2px 6px rgba(0,0,0,0.1);"
    });
    $('#tab1').append(newDiv);

    $('#tab3 > div > div:first').empty();
    $('#remark').val('');
    $('#callBackDateOutcome').val('');
    $('#disposition').val('');
    $('#subDisposition').val('');
    $('#subSubDisposition').val('');

    $('#tab3').find('input, select, textarea').css('border', '1px solid #ccc');
}
function makeCall() {
    const phone = document.getElementById("phoneInput").value.trim();
   
    if (!/^\d{10}$/.test(phone)) { showWarning_New("Phone number must be exactly 10 digits."); return; }
    clearFeilds();
    callAPI("/api/Genesys/makecall", "POST", { phone });
}

function openConferencePrompt() {
    Swal.fire({
        title: 'Enter conference number',
        input: 'text',
        inputPlaceholder: 'e.g. 9021292629',
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
        url: '/api/Genesys/GetDispositions',
        type: 'GET',
        data: { empCode: agentId },
        success: function (response) {
            const dispositions = response.dispositions || [];
            dispo = response;

            console.log("Level Disposition : " + JSON.stringify(dispo));
            const dispositionSelect = $('#disposition');
            dispositionSelect.empty();
            dispositionSelect.append('<option value="">-- Select Disposition --</option>');

            dispositions.forEach(disposition => {
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
    const subSubDispoDiv = document.getElementById("SubSubDispo");
    const subSubDispoSelect = document.getElementById("subSubDisposition");
    const callBackDateDiv = document.getElementById("callBackDateDiv");

    subDispoSelect.innerHTML = '<option value="">-- Select Sub Disposition --</option>';
    subSubDispoSelect.innerHTML = '<option value="">-- Select Sub SubDisposition --</option>';
    subSubDispoDiv.style.display = "none";


    if (callBackDateDiv) {
        callBackDateDiv.style.display = "none";
    }

    if (dispoId) {
        const selectedOption = document.querySelector(`#disposition option[value="${dispoId}"]`);
        const dispType = selectedOption ? selectedOption.getAttribute("data-disp-type") : null;

        const selectedDisposition = dispo.dispositions.find(d => d.id === parseInt(dispoId));

        if (selectedDisposition) {
            const subDispositions = selectedDisposition.subDispositions;


            if (subDispositions && subDispositions.length > 0) {
                subDispositions.forEach(sub => {
                    const option = document.createElement("option");
                    option.value = sub.id;
                    option.textContent = sub.name;
                    subDispoSelect.appendChild(option);
                });


                document.getElementById("SubDispositiondiv").style.display = "flex";
            } else {

                document.getElementById("SubDispositiondiv").style.display = "none";
            }


            //if (dispType === "PCB" || dispType === "CCB") {
            //    if (callBackDateDiv) {
            //        callBackDateDiv.style.display = "flex";
            //    }
            //}
        }
    }
}

function updateSubSubDisposition() {
    const dispoId = document.getElementById("disposition").value;
    const subDispoId = document.getElementById("subDisposition").value;
    const subSubDispoDiv = document.getElementById("SubSubDispo");
    const subSubDispoSelect = document.getElementById("subSubDisposition");
    const callBackDateDiv = document.getElementById("callBackDateDiv");
    subSubDispoSelect.innerHTML = '<option value="">-- Select Sub SubDisposition --</option>';
    subSubDispoDiv.style.display = "none";

    if (callBackDateDiv) {
        callBackDateDiv.style.display = "none";
    }

    if (dispoId && subDispoId) {

        const selectedDisposition = dispo.dispositions.find(d => d.id === parseInt(dispoId));

        if (!selectedDisposition) {
            console.log("No matching disposition found.");
            return;
        }

        const selectedSubDispo = selectedDisposition.subDispositions.find(sd => sd.id === parseInt(subDispoId));

        if (!selectedSubDispo) {
            console.log("No matching sub disposition found.");
            return;
        }

        var str = JSON.stringify(selectedSubDispo);
        var obj = JSON.parse(str);
        var subdisptype = obj.disP_TYPE;
        subdisptypesave = subdisptype;

        if (subdisptype === "PCB" || subdisptype === "CCB") {
            if (callBackDateDiv) {
                callBackDateDiv.style.display = "flex";
            }
        }


        if (selectedSubDispo.subSubDispositions && selectedSubDispo.subSubDispositions.length > 0) {

            selectedSubDispo.subSubDispositions.forEach(subSub => {
                const option = document.createElement("option");
                option.value = subSub.id;
                option.textContent = subSub.name;
                subSubDispoSelect.appendChild(option);
            });

            subSubDispoDiv.style.display = "flex";
        } else {

            subSubDispoDiv.style.display = "none";
        }
    } else {
        console.log("Either disposition or sub disposition is not selected.");
    }
}



//document.getElementById("disposition").addEventListener("change", function () {
//    updateSubDisposition();
//    toggleCallBackDateField();
//});


document.addEventListener("DOMContentLoaded", function () {
    var disposition = document.getElementById("disposition");
    if (disposition) {
        disposition.addEventListener("change", function () {
            updateSubDisposition();
            toggleCallBackDateField();
        });
    }
});


document.addEventListener("DOMContentLoaded", function () {
    var disposition = document.getElementById("subDisposition");
    if (disposition) {
        disposition.addEventListener("change", function () {
            updateSubSubDisposition();
            toggleCallBackDateField();
        });
    }
});



function submitDisposition() {
    let agentStatus = $('#status').text().trim().toUpperCase();

    if (agentStatus !== "WRAPING") {
        Swal.fire({
            icon: 'warning',
            title: 'Action Blocked',
            text: 'Please disconnect the call before submitting.',
            confirmButtonColor: '#d33'
        });
        return;
    }


    let formData = {};
    let isValid = true;
    let validationMessage = '';

    $('#tab3').find('input, select, textarea').each(function () {
        const field = $(this);
        if (!field.is(':visible')) {
            return;
        }

        let fieldName = field.attr('name') || field.attr('id');
        let isRequired = field.prop('required') || field.closest('label').find('span[style*="red"]').length > 0;
        let fieldType = field.attr('type');
        let fieldValue = '';
        if (field.is('select')) {

            fieldValue = field.find('option:selected').text().trim();


            if (fieldValue === 'Select' || fieldValue === '-- Select --' || fieldValue === '') {
                fieldValue = '';
            }

        } else if (fieldType === 'checkbox') {
            fieldValue = field.prop('checked');
        } else if (fieldType === 'radio') {
            const selected = $(`input[name="${field.attr('name')}"]:checked`);
            fieldValue = selected.length > 0 ? selected.val() : '';
        } else {
            fieldValue = field.val()?.trim();
        }

        if (isRequired && !fieldValue) {
            isValid = false;
            validationMessage += `${fieldName} is required.\n`;
            field.css('border', '1px solid red');
        } else {
            field.css('border', '1px solid #ccc');
        }

        if (fieldName) {
            formData[fieldName] = fieldValue;
        }
    });

    const manualFields = [
        { id: 'disposition', required: true },
        { id: 'subDisposition', required: true },
        { id: 'subSubDisposition', required: true },
        { id: 'callBackDateOutcome', required: true },
        { id: 'remark', required: true }
    ];

    manualFields.forEach(field => {
        const $el = $(`#${field.id}`);

        if (!$el.length || !$el.is(':visible')) return;

        const value = $el.val()?.trim();

        if (field.required && !value) {
            isValid = false;
            validationMessage += `${field.id} is required.\n`;
            $el.css('border', '1px solid red');
        } else {
            $el.css('border', '1px solid #ccc');
        }

        formData[field.id] = value;
    });


    if (!isValid) {
        showWarning_New("Please fill required fields:\n\n" + validationMessage);
        return;
    }

    console.log("Sub,mite data : " + JSON.stringify(formData));

    let disptypeKey = "dispTypeKey";
    let dispoId = document.getElementById("disposition").value;
    let selectedOption = document.querySelector(`#disposition option[value="${dispoId}"]`);
    let dispType = selectedOption ? selectedOption.getAttribute("data-disp-type") : null;
    formData[disptypeKey] = subdisptypesave;


    // ADD partyId into final payload
    formData["partyId"] = selectedPartyId;

    formData["entity"] = entityId;

    console.log("Submitting data:", JSON.stringify(formData));
    $.ajax({
        url: '/api/Genesys/submit/',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        beforeSend: function () {

            $('#loader').show();
        },
        success: function (response) {
            $('#loader').hide();
            clearFeilds();

            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: 'Form submitted successfully!',
                confirmButtonColor: '#3085d6'
            });
            openTab({ currentTarget: document.querySelector('.nav-tab.active .nav-tab-text') }, 'tab1');

        },
        error: function (error) {
            alert('Error submitting form.');
            console.error('Submission error:', error);
        }
    });
}


function searchPhoneNumber() {

    var phoneNumber = document.getElementById("phoneSearch").value;

    if (!phoneNumber) {
        showWarning_Required("Please Enter Mobile Number");
        return;
    }


    console.log("Searching for:", phoneNumber);

    fetch("/api/Genesys/searchPhoneNumber", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ phone: phoneNumber })
    })
        .then(response => {
            if (response.ok) {
                console.log("Search request sent successfully");
                return response.json(); 
            }
        })
        .then(data => {
            console.log("API Response:", data);
           
            document.getElementById("entityframe").innerHTML = JSON.stringify(data);
        })
        .catch(error => {
            console.error("Error:", error);
        });
}


function GetCustomerType() {

    let formDatas = {};
    var CustomerType = TypeOfcustomer;

    captureFields.forEach(field => {
       
        if (field.FieldName === "Name" || field.FieldName === "Mobile_Number") {

            const value = $(`input[name='${field.FieldName}']`).val();
          
            formDatas[field.FieldName] = value;
        }

    });
    var FirstName = formDatas.Name;
    var MobileNO = formDatas.Mobile_Number;

    console.log("Collected Values:", formDatas);

    if (!CustomerType) {
        showWarning_Required("Please Select Custonmer Type");
        return;
    }

    if (!FirstName) {
        showWarning_Required("Please Enter The Name");
        return;
    }

    if (!MobileNO) {
        showWarning_Required("Please Enter Mobile Number");
        return;
    }

    fetch("/api/Genesys/CustomerType", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            Type: CustomerType,
            Name: FirstName,
            Mobile: MobileNO })
        })
        .then(response => {
            if (response.ok) {
                console.log("Search request sent successfully");
                return response.json();
            }
        })
       
        .catch(error => {
            console.error("Error:", error);
        });
   
}


function showWarning_Required(message) {
    Swal.fire({
        icon: 'warning',
        title: 'Warning',
        text: message,
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 50000,
        timerProgressBar: true,
        background: '#fff7ed',
        color: '#92400e',
        iconColor: '#f59e0b',
        customClass: {
            popup: 'shadow-lg rounded-xl'
        }
    });
}

function showSuccess(message) {
    Swal.fire({
        icon: 'success', 
        title: 'Success',
        text: message,
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 50000,
        timerProgressBar: true,
        background: '#d1fae5', 
        color: '#065f46',      
        iconColor: '#10b981',  
        customClass: {
            popup: 'shadow-lg rounded-xl'
        }
    });
}




