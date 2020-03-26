
$(function () {

    var currentRequiredId = 0;
    var currentDesiredId = 0;

    var requiredModal = $("#req").parents(".modal-content");
    var desiredModal = $("#des").parents(".modal-content");

    //check for previous saved requirements and add
    var addSavedRequirementsToTable = function () {

        //check for saved Requirements
        var $rows = $("#savedRequirements").find("tr");

        $rows.each(function () {
            var $hidden = $("#baseRequirement").clone();

            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentRequiredId));
                $(this).prop("name", $(this).prop("name").replace("0", currentRequiredId));
            });
            //Add the info to the cloned hidden fields, add a row to the table
            $(this).find("td").each(function () {
                $hidden.find("input[name$='" + $(this).prop("class") + "']").val($(this).html());
            });
            $hidden.find("input[name$='RequirementIsRequired']").val("true");

            $('.order-requirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $(this).find(".SavedReqTypeName").html() + '</td>' +
                '<td class="table-description-column">' + $(this).find(".RequirementDescription").html() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-requirement-row bold">&times;</span></td>' +
                '</tr>');
            currentRequiredId++;
        });
        $('.order-requirement-list').removeClass("d-none");
    };

    //check for previous saved desires and add
    var addSavedsDesiredRequirementsToTable = function () {

        var $rows = $("#savedDesiredRequirements").find("tr");

        $rows.each(function () {
            var $hidden = $("#baseDesiredRequirement").clone();

            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentDesiredId));
                $(this).prop("name", $(this).prop("name").replace("0", currentDesiredId));
            });
            //Add the info to the cloned hidden fields, add a row to the table
            $(this).find("td").each(function () {
                $hidden.find("input[name$='" + $(this).prop("class") + "']").val($(this).html());
            });
            //$hidden.find("input[name$='RequirementIsRequired']").val("true");

            $('.order-desiredRequirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $(this).find(".SavedReqTypeName").html() + '</td>' +
                '<td class="table-description-column">' + $(this).find(".DesiredRequirementDescription").html() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-desiredRequirement-row bold">&times;</span></td>' +
                '</tr>');
            currentDesiredId++;
        });
        $('.order-desiredRequirement-list').removeClass("d-none");
    };

    addSavedRequirementsToTable();
    addSavedsDesiredRequirementsToTable();

    $("body").on("click", ".remove-requirement-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentRequiredId = 0;
        if ($rows.length === 0) {
            $('.order-requirement-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentRequiredId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentRequiredId));
                });
                currentRequiredId++;
            });
        }
    });

    $("body").on("click", ".remove-desiredRequirement-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentDesiredId = 0;
        if ($rows.length === 0) {
            $('.order-desiredRequirement-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentDesiredId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentDesiredId));
                });
                currentDesiredId++;
            });
        }
    });

    $("body").on("click", ".add-requirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#RequirementType"), target.find("input[name$=Gender]").parents(".form-group"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".add-desiredRequirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#DesiredRequirementType"), target.find("input[name$=DesiredGender]").parents(".form-group"), target.find("#DesiredRequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".save-requirement", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        if (modalContent.find("#RequirementType option:selected").text() === "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() === "Female" ? "Kvinna" : "Man";
            modalContent.find("#RequirementDescription").val(textToUse);
        }
        //Validate the form
        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentRequiredId));
                $(this).prop("name", $(this).prop("name").replace("0", currentRequiredId));
            });
            currentRequiredId++;
            modalContent.find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            modalContent.find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });
            //Add the info to the cloned hidden fields, add a row to the table
            $('.order-requirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $("#RequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $("#RequirementDescription").val() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-requirement-row bold">&times;</span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-requirement-list').removeClass("d-none");
            //Close dialog
            $("#addRequirement").modal("hide");
        }
    });

    var toggleGender = function (reqType, genderSpan, descriptionSpan) {
        if (reqType.val() === "Gender") {
            genderSpan.show();
            descriptionSpan.hide();
        }
        else {
            genderSpan.hide();
            descriptionSpan.show();
            descriptionSpan.find("input").val("");
        }
    };

    $("body").on("click", ".save-desiredRequirement", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        if (modalContent.find("#DesiredRequirementType option:selected").text() === "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() === "Female" ? "Kvinna" : "Man";
            modalContent.find("#DesiredRequirementDescription").val(textToUse);
        }
        //Validate the form
        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseDesiredRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentDesiredId));
                $(this).prop("name", $(this).prop("name").replace("0", currentDesiredId));
            });
            currentDesiredId++;
            modalContent.find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            modalContent.find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });

            //Add the info to the cloned hidden fields, add a row to the table
            $('.order-desiredRequirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $(this).parents(".modal-content").find("#DesiredRequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $(this).parents(".modal-content").find("#DesiredRequirementDescription").val() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-desiredRequirement-row bold">&times;</span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-desiredRequirement-list').removeClass("d-none");
            //Close dialog
            $("#addDesiredRequirement").modal("hide");
        }
    });

    requiredModal.find("#RequirementType").on("change", function () {
        toggleGender(requiredModal.find("#RequirementType"), requiredModal.find("input[name$=Gender]").parents(".form-group"), requiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    desiredModal.find("#DesiredRequirementType").on("change", function () {
        toggleGender(desiredModal.find("#DesiredRequirementType"), desiredModal.find("input[name$=DesiredGender]").parents(".form-group"), desiredModal.find("#DesiredRequirementDescription").parents(".form-group"));
    });

    function AddRequirement(target) {
        target.find("#RequirementDescription").val("");
        var $form = target.find('form:first');
        target.bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
    }
});