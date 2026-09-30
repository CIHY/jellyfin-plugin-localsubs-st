function controller(view, params) {
    const apis = {
        template: {
            getList: "GetTemplates",
            add: "AddTemplate",
            delete: "DeleteTemplates",
            reset: "ResetTemplates"
        },
        librarySettings: {
            getList: "GetLibrariesSettings",
            postData: "UpdateLibrarySettings"
        }
    };

    function logNetReqDebug(funcName, apiName, data) {
        console.log("LocalSubs => " + funcName + "::" + apiName, data);
    }

    function logNetReqError(funcName, apiName, err) {
        console.error("LocalSubs => " + funcName + "::" + apiName, err);
    }

    function getUrl(apiName) {
        return ApiClient.getUrl("Jellyfin.Plugin.LocalSubs/" + apiName);
    }

    function isElement(target) {
        return target instanceof Element;
    }

    function registerEvent(elem, evtName, evtHandle) {
        if (!isElement(elem)) {
            return;
        }

        elem.addEventListener(evtName, evtHandle);
    }

    function clearChildren(elem) {
        if (isElement(elem)) {
            while (elem.firstChild) elem.removeChild(elem.lastChild);
        }
    }

    function buildStdCheckbox(name, value, checked, text) {
        const label = document.createElement("label");

        const input = document.createElement("input");
        input.type = "checkbox";
        input.name = name;
        input.value = value;
        input.checked = checked;
        input.setAttribute("is", "emby-checkbox");
        input.classList.add("checkbox", "checkboxContainer-withDescription", "templateCheckbox");
        label.appendChild(input);

        const span = document.createElement("span");
        span.textContent = text;
        label.appendChild(span);

        return label;
    }

    function ctrlTemplates(form) {
        if (!isElement(form)) {
            return;
        }

        const checkboxName = "LocalSubs_TemplatesForm_ExistsTemplate";

        function getNewTemplate() {
            const newTemplateInput = form.LocalSubs_TemplatesForm_AddInput;
            if (!isElement(newTemplateInput)) {
                return null;
            }

            const value = !newTemplateInput.value ? null : newTemplateInput.value.toString().trim();
            return !value ? null : value;
        }

        function reloadTemplateList() {
            const templateListElem = form.querySelector("#LocalSubs_TemplatesForm_Templates");
            if (!isElement(templateListElem)) {
                return;
            }

            Dashboard.showLoadingMsg();

            const apiName = apis.template.getList;
            ApiClient.ajax({
                type: "GET",
                url: getUrl(apiName),
                contentType: "application/json",
                dataType: "json"
            })
                .then(function (resp) {
                    logNetReqDebug("reloadTemplateList", apiName, resp);
                    clearChildren(templateListElem);

                    const templateList = resp;
                    if (!(templateList instanceof Array) || templateList.length == 0) {
                        const holderSpan = document.createElement("span");
                        holderSpan.textContent = "Empty";
                        templateListElem.appendChild(holderSpan);

                        Dashboard.hideLoadingMsg();
                        return;
                    }

                    const itemsContainer = document.createElement("div");
                    itemsContainer.dataset.role = "controlgroup";
                    for (let i = 0; i < templateList.length; i++) {
                        const template = templateList[i];
                        itemsContainer.appendChild(buildStdCheckbox(checkboxName, template, false, template));
                    }
                    templateListElem.appendChild(itemsContainer);

                    Dashboard.hideLoadingMsg();
                })
                .catch(function (err) {
                    logNetReqError("reloadTemplateList", apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                });
        }

        registerEvent(view, "viewshow", function (evt) {
            reloadTemplateList();
        });

        registerEvent(form.LocalSubs_TemplatesForm_AddButton, "click", function (evt) {
            evt.preventDefault();

            const newTemplate = getNewTemplate();
            if (!newTemplate) {
                return;
            }

            Dashboard.showLoadingMsg();

            const apiName = apis.template.add;
            ApiClient.ajax({
                type: "POST",
                url: getUrl(apiName),
                data: JSON.stringify({ Template: newTemplate }),
                contentType: "application/json"
            })
                .then(function (resp) {
                    logNetReqDebug("TemplatesForm_AddButton_" + evt.type, apiName, resp);
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                })
                .catch(function (err) {
                    logNetReqError("TemplatesForm_AddButton_" + evt.type, apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                });
        });

        registerEvent(form.LocalSubs_TemplatesForm_DeleteButton, "click", function (evt) {
            evt.preventDefault();

            const checkboxs = form[checkboxName];
            const values = [];
            for (var i = 0; i < checkboxs.length; i++) {
                const item = checkboxs[i];
                if (item.checked) {
                    values.push(item.value);
                }
            }

            if (values.length == 0) {
                return;
            }

            Dashboard.showLoadingMsg();

            const apiName = apis.template.delete;
            ApiClient.ajax({
                type: "POST",
                url: getUrl(apiName),
                data: JSON.stringify(values),
                contentType: "application/json"
            })
                .then(function (resp) {
                    logNetReqDebug("TemplatesForm_DeleteButton_" + evt.type, apiName, resp);
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                })
                .catch(function (err) {
                    logNetReqError("TemplatesForm_DeleteButton_" + evt.type, apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                });
        });

        registerEvent(form.LocalSubs_TemplatesForm_ResetButton, "click", function (evt) {
            evt.preventDefault();
            Dashboard.showLoadingMsg();

            const apiName = apis.template.reset;
            ApiClient.ajax({
                type: "POST",
                url: getUrl(apiName)
            })
                .then(function (resp) {
                    logNetReqDebug("TemplatesForm_ResetButton_" + evt.type, apiName, resp);
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                })
                .catch(function (err) {
                    logNetReqError("TemplatesForm_ResetButton_" + evt.type, apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                    reloadTemplateList();
                });
        });
    }
    ctrlTemplates(view.querySelector("#LocalSubs_TemplatesForm"));

    function ctrlLibrariesSettings(form) {
        if (!isElement(form)) {
            return;
        }

        const childFormPrefix = "LocalSubs_LibrariesSettingsForm_ChildForm_";
        const dataTemplate = {
            LibraryId: "LibraryId",
            SubtitleLangsISO: "SubtitleLangsISO",
            SkipIfHaveEmbedded: "SkipIfHaveEmbedded",
            SkipIfHaveMatchingAudioTracks: "SkipIfHaveMatchingAudioTracks",
            IsEnabled: "IsEnabled",
            // display: LibraryName
        };
        const saveButtonTemplate = form.querySelector("#LocalSubs_LibrariesSettingsForm_SaveButtonTemplate");
        const cultures = [];

        function buildCultureSelect(name, selected) {
            if (selected instanceof String) {
                selected = [selected];
            }
            if (!(selected instanceof Array)) {
                selected = [];
            }

            const container = document.createElement("div");
            container.classList.add("checkboxList");
            container.style.height = "300px";
            container.style.overflowY = "auto";

            for (let i = 0; i < cultures.length; i++) {
                const item = cultures[i];

                const itemEl = buildStdCheckbox(name, item.code, selected.indexOf(item.code) >= 0, item.name);
                itemEl.classList.add("emby-checkbox-label");
                container.appendChild(itemEl);
            }

            return container;
        }

        function getPostData(childForm) {
            const postData = JSON.parse(JSON.stringify(dataTemplate));
            postData.LibraryId = childForm[childFormPrefix + dataTemplate.LibraryId].value;
            postData.SkipIfHaveEmbedded = childForm[childFormPrefix + dataTemplate.SkipIfHaveEmbedded].checked;
            postData.SkipIfHaveMatchingAudioTracks = childForm[childFormPrefix + dataTemplate.SkipIfHaveMatchingAudioTracks].checked;
            postData.IsEnabled = childForm[childFormPrefix + dataTemplate.IsEnabled].checked;
            postData.SubtitleLangsISO = [];

            const langStringEls = childForm[childFormPrefix + dataTemplate.SubtitleLangsISO];
            for (let i = 0; i < langStringEls.length; i++) {
                const item = langStringEls[i];
                if (item.checked) {
                    postData.SubtitleLangsISO.push(item.value);
                }
            }

            return postData;
        }

        function reloadSettingsList() {
            const settingsListElem = form.querySelector("#LocalSubs_LibrariesSettingsForm_LibrariesSettings");
            if (!isElement(settingsListElem)) {
                return;
            }

            Dashboard.showLoadingMsg();

            const apiName = apis.librarySettings.getList;
            ApiClient.ajax({
                type: "GET",
                url: getUrl(apiName),
                dataType: "json"
            })
                .then(function (resp) {
                    logNetReqDebug("reloadSettingsList", apiName, resp);
                    clearChildren(settingsListElem);

                    const settingsList = resp;
                    if (!(settingsList instanceof Array) || settingsList.length == 0) {
                        Dashboard.hideLoadingMsg();
                        return;
                    }

                    for (let i = 0; i < settingsList.length; i++) {
                        const item = settingsList[i];

                        const container = document.createElement("form");
                        container.classList.add("paperList");
                        container.style.padding = "1rem";
                        container.style.margin = "0";

                        const idHidden = document.createElement("input");
                        idHidden.type = "hidden";
                        idHidden.name = childFormPrefix + dataTemplate.LibraryId;
                        idHidden.value = item.LibraryId;
                        container.appendChild(idHidden);

                        const nameH4 = document.createElement("h4");
                        nameH4.classList.add("checkboxListLabel");
                        nameH4.style.margin = "0 0 1.5em 0";
                        nameH4.textContent = "Library: " + item.LibraryName;
                        container.appendChild(nameH4);

                        const enableCheckbox = buildStdCheckbox(childFormPrefix + dataTemplate.IsEnabled, 1, item.IsEnabled, "Enable local subtitle search");
                        enableCheckbox.classList.add("checkboxContainer");
                        enableCheckbox.style.margin = "0 0 1em 0";
                        container.appendChild(enableCheckbox);

                        const subtitleLangsLabel = document.createElement("label");
                        subtitleLangsLabel.style.display = "block";
                        subtitleLangsLabel.style.margin = "0 0 0.7rem 0";
                        subtitleLangsLabel.textContent = "Search languages";
                        const subtitleLangsCheckbox = buildCultureSelect(childFormPrefix + dataTemplate.SubtitleLangsISO, item.SubtitleLangsISO);
                        subtitleLangsCheckbox.style.padding = "0 1rem";
                        subtitleLangsCheckbox.style.border = "1px solid #3b3b3b";
                        const subtitleLangsContainer = document.createElement("div");
                        subtitleLangsContainer.style.margin = "0 0 1.8em 0";
                        subtitleLangsContainer.appendChild(subtitleLangsLabel);
                        subtitleLangsContainer.appendChild(subtitleLangsCheckbox);
                        container.appendChild(subtitleLangsContainer);

                        const skipEmbeddedCheckbox = buildStdCheckbox(childFormPrefix + dataTemplate.SkipIfHaveEmbedded, 1, item.SkipIfHaveEmbedded, "Skip if the video already contains embedded subtitles");
                        skipEmbeddedCheckbox.classList.add("checkboxContainer");
                        container.appendChild(skipEmbeddedCheckbox);

                        const skipAudioMatchedCheckbox = buildStdCheckbox(childFormPrefix + dataTemplate.SkipIfHaveMatchingAudioTracks, 1, item.SkipIfHaveMatchingAudioTracks, "Skip if the default audio track matches the download language");
                        skipAudioMatchedCheckbox.classList.add("checkboxContainer");
                        container.appendChild(skipAudioMatchedCheckbox);

                        const saveButton = saveButtonTemplate.cloneNode(true);
                        saveButton.type = "submit";
                        const bottomDiv = document.createElement("div");
                        bottomDiv.appendChild(saveButton);
                        container.appendChild(bottomDiv);

                        settingsListElem.appendChild(container);
                    }

                    Dashboard.hideLoadingMsg();
                })
                .catch(function (err) {
                    logNetReqError("reloadSettingsList", apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                });
        }

        registerEvent(view, "viewshow", function (evt) {
            const culturesApi = "Localization/cultures";
            ApiClient.ajax({
                type: "GET",
                url: ApiClient.getUrl(culturesApi),
                dataType: "json"
            })
                .then(function (resp) {
                    logNetReqDebug("ctrlLibrariesSettings_init", culturesApi, resp);

                    const list = resp;
                    if (!(list instanceof Array) || list.length == 0) {
                        return;
                    }

                    let result = [];
                    for (let i = 0; i < list.length; i++) {
                        result.push(list[i].ThreeLetterISOLanguageName);
                    }

                    result.sort(function (a, b) {
                        return a.localeCompare(b);
                    });

                    result = Array.from(new Set(result));
                    for (let i = 0; i < result.length; i++) {
                        const code = result[i];
                        const name = (list.find(function (item) {
                            return item.ThreeLetterISOLanguageName == code;
                        }) || { DisplayName: code }).DisplayName;

                        cultures.push({ code, name });
                    }

                    cultures.sort(function (a, b) {
                        return a.name.localeCompare(b.name);
                    });

                    reloadSettingsList();
                })
                .catch(function (err) {
                    logNetReqError("ctrlLibrariesSettings_init", culturesApi, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                });
        });

        registerEvent(form, "submit", function (evt) {
            evt.preventDefault();

            const container = evt.target;
            if (!isElement(container) || container.nodeName.toLowerCase() != "form") {
                return;
            }

            Dashboard.showLoadingMsg();

            const apiName = apis.librarySettings.postData;
            ApiClient.ajax({
                type: "POST",
                url: getUrl(apiName),
                data: JSON.stringify(getPostData(evt.target)),
                contentType: "application/json"
            })
                .then(function (resp) {
                    logNetReqError("ctrlLibrariesSettings_init", apiName, resp);
                    Dashboard.hideLoadingMsg();
                    reloadSettingsList();
                })
                .catch(function (err) {
                    logNetReqError("ctrlLibrariesSettings_init", apiName, err);
                    Dashboard.processErrorResponse({ statusText: "Failed" });
                    Dashboard.hideLoadingMsg();
                    reloadSettingsList();
                });
        });
    }
    ctrlLibrariesSettings(view.querySelector("#LocalSubs_LibrariesSettingsForm"));
};

export default controller;
